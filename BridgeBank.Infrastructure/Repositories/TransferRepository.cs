using BridgeBank.Application.Repositories.Interfaces;
using BridgeBank.Application.Transfers.Models;
using BridgeBank.Domain.Entities;
using BridgeBank.Domain.Enums;
using BridgeBank.Infrastructure.Database;
using Dapper;

namespace BridgeBank.Infrastructure.Repositories;

public class TransferRepository : ITransferRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public TransferRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private sealed record ExistingTransfer(Guid Id, string DestinationAccountNumber, decimal Amount);

    public async Task<TransferResult> TransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        string destinationAccountNumber,
        decimal amount,
        string? description,
        string clientReference)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        const string destinationIdSql = """
            SELECT Id FROM Accounts WHERE AccountNumber = @DestinationAccountNumber;
            """;

        var destinationId = await connection.ExecuteScalarAsync<Guid?>(
            destinationIdSql, new { DestinationAccountNumber = destinationAccountNumber }, dbTransaction);

        if (destinationId is null)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.DestinationAccountNotFound);
        }

        if (destinationId == sourceAccountId)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.SameAccount);
        }

        // Both rows are locked in Id order, so two transfers moving money in
        // opposite directions cannot deadlock on each other's account.
        const string selectSql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts WITH (UPDLOCK, ROWLOCK)
            WHERE Id IN (@SourceAccountId, @DestinationId)
            ORDER BY Id;
            """;

        var accounts = (await connection.QueryAsync<Account>(
            selectSql,
            new { SourceAccountId = sourceAccountId, DestinationId = destinationId.Value },
            dbTransaction)).ToDictionary(account => account.Id);

        if (!accounts.TryGetValue(sourceAccountId, out var source) || source.CustomerId != customerId)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.SourceAccountNotFound);
        }

        if (!accounts.TryGetValue(destinationId.Value, out var destination))
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.DestinationAccountNotFound);
        }

        const string existingSql = """
            SELECT Id, DestinationAccountNumber, Amount
            FROM Transfers
            WHERE SourceAccountId = @SourceAccountId AND ClientReference = @ClientReference;
            """;

        var existing = await connection.QuerySingleOrDefaultAsync<ExistingTransfer>(
            existingSql,
            new { SourceAccountId = sourceAccountId, ClientReference = clientReference },
            dbTransaction);

        if (existing is not null)
        {
            dbTransaction.Rollback();
            return existing.Amount == amount && existing.DestinationAccountNumber == destination.AccountNumber
                ? new TransferResult(TransferOutcome.Success, existing.Id, source.Balance, destination.Balance)
                : new TransferResult(TransferOutcome.ClientReferenceReused);
        }

        if (source.AccountStatus != AccountStatus.Active || destination.AccountStatus != AccountStatus.Active)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.AccountInactive);
        }

        if (amount > source.Balance)
        {
            dbTransaction.Rollback();
            return new TransferResult(TransferOutcome.InsufficientFunds);
        }

        source.Withdraw(amount);
        destination.Deposit(amount);

        const string updateSql = """
            UPDATE Accounts
            SET Balance = @Balance
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(updateSql, new { source.Balance, source.Id }, dbTransaction);
        await connection.ExecuteAsync(updateSql, new { destination.Balance, destination.Id }, dbTransaction);

        var sourceTransaction = new Transaction(
            source.Id, TransactionType.Debit, TransactionCategory.Transfer, amount, description,
            clientReference);

        var destinationTransaction = new Transaction(
            destination.Id, TransactionType.Credit, TransactionCategory.Transfer, amount, description,
            null);

        const string insertTransactionSql = """
            INSERT INTO Transactions
            (Id, AccountId, TransactionType, TransactionCategory, Amount, Description, CreatedAt,
             ClientReference)
            VALUES
            (@Id, @AccountId, @TransactionType, @TransactionCategory, @Amount, @Description, @CreatedAt,
             @ClientReference);
            """;

        await connection.ExecuteAsync(
            insertTransactionSql,
            new[] { sourceTransaction, destinationTransaction },
            dbTransaction);

        var transfer = new Transfer(
            source.Id,
            destination.AccountNumber,
            string.Empty,
            amount,
            TransferType.Internal)
        {
            ClientReference = clientReference
        };

        transfer.Complete();

        const string insertTransferSql = """
            INSERT INTO Transfers
            (Id, SourceAccountId, DestinationAccountNumber, DestinationBankCode,
             Amount, TransferType, Status, CreatedAt, ClientReference)
            VALUES
            (@Id, @SourceAccountId, @DestinationAccountNumber, NULLIF(@DestinationBankCode, ''),
             @Amount, @TransferType, @Status, @CreatedAt, @ClientReference);
            """;

        await connection.ExecuteAsync(insertTransferSql, transfer, dbTransaction);

        dbTransaction.Commit();

        return new TransferResult(
            TransferOutcome.Success,
            transfer.Id,
            source.Balance,
            destination.Balance);
    }

    private sealed record ExistingExternalTransfer(Guid Id, TransferStatus Status);

    // The debit and the Pending row are committed before the peer is called: holding the account
    // lock across a network call would block every other operation on it.
    public async Task<ExternalTransferBegin> BeginExternalTransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        string destinationAccountNumber,
        string destinationBankCode,
        decimal amount,
        string? description,
        string clientReference)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        const string readSql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts WITH (UPDLOCK, ROWLOCK)
            WHERE Id = @SourceAccountId AND CustomerId = @CustomerId;

            SELECT Id, Status FROM Transfers
            WHERE SourceAccountId = @SourceAccountId AND ClientReference = @ClientReference;
            """;

        using var results = await connection.QueryMultipleAsync(
            readSql,
            new
            {
                SourceAccountId = sourceAccountId,
                CustomerId = customerId,
                ClientReference = clientReference
            },
            dbTransaction);

        var source = await results.ReadSingleOrDefaultAsync<Account>();
        var existing = await results.ReadSingleOrDefaultAsync<ExistingExternalTransfer>();

        if (source is null)
        {
            dbTransaction.Rollback();
            return new ExternalTransferBegin(TransferOutcome.SourceAccountNotFound);
        }

        // A replay hands back the stored transfer and its status instead of debiting again.
        if (existing is not null)
        {
            dbTransaction.Rollback();
            return new ExternalTransferBegin(
                TransferOutcome.Success, existing.Id, source.AccountNumber, source.Balance, existing.Status);
        }

        if (source.AccountStatus != AccountStatus.Active)
        {
            dbTransaction.Rollback();
            return new ExternalTransferBegin(TransferOutcome.AccountInactive);
        }

        if (amount > source.Balance)
        {
            dbTransaction.Rollback();
            return new ExternalTransferBegin(TransferOutcome.InsufficientFunds);
        }

        source.Withdraw(amount);

        const string updateSql = """
            UPDATE Accounts
            SET Balance = @Balance
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(updateSql, new { source.Balance, source.Id }, dbTransaction);

        var transaction = new Transaction(
            source.Id, TransactionType.Debit, TransactionCategory.Transfer, amount, description,
            clientReference);

        const string insertTransactionSql = """
            INSERT INTO Transactions
            (Id, AccountId, TransactionType, TransactionCategory, Amount, Description, CreatedAt,
             ClientReference)
            VALUES
            (@Id, @AccountId, @TransactionType, @TransactionCategory, @Amount, @Description, @CreatedAt,
             @ClientReference);
            """;

        await connection.ExecuteAsync(insertTransactionSql, transaction, dbTransaction);

        // Stays Pending until the peer answers.
        var transfer = new Transfer(
            source.Id, destinationAccountNumber, destinationBankCode, amount, TransferType.External)
        {
            ClientReference = clientReference
        };

        const string insertTransferSql = """
            INSERT INTO Transfers
            (Id, SourceAccountId, DestinationAccountNumber, DestinationBankCode,
             Amount, TransferType, Status, CreatedAt, ClientReference)
            VALUES
            (@Id, @SourceAccountId, @DestinationAccountNumber, NULLIF(@DestinationBankCode, ''),
             @Amount, @TransferType, @Status, @CreatedAt, @ClientReference);
            """;

        await connection.ExecuteAsync(insertTransferSql, transfer, dbTransaction);

        dbTransaction.Commit();

        return new ExternalTransferBegin(
            TransferOutcome.Success, transfer.Id, source.AccountNumber, source.Balance);
    }

    public async Task CompleteExternalTransferAsync(Guid transferId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE Transfers
            SET Status = @Status
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(sql, new { Status = TransferStatus.Completed, Id = transferId });
    }

    // The peer refused, so the amount never left: put it back and record the transfer as failed.
    public async Task FailExternalTransferAsync(Guid transferId)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        const string readSql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts WITH (UPDLOCK, ROWLOCK)
            WHERE Id = (SELECT SourceAccountId FROM Transfers WHERE Id = @TransferId);

            SELECT Amount FROM Transfers WHERE Id = @TransferId;
            """;

        using var results = await connection.QueryMultipleAsync(
            readSql, new { TransferId = transferId }, dbTransaction);

        var source = await results.ReadSingleOrDefaultAsync<Account>();
        var amount = await results.ReadSingleOrDefaultAsync<decimal>();

        if (source is null)
        {
            dbTransaction.Rollback();
            return;
        }

        source.Deposit(amount);

        const string updateBalanceSql = """
            UPDATE Accounts
            SET Balance = @Balance
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(updateBalanceSql, new { source.Balance, source.Id }, dbTransaction);

        const string updateTransferSql = """
            UPDATE Transfers
            SET Status = @Status
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(
            updateTransferSql, new { Status = TransferStatus.Failed, Id = transferId }, dbTransaction);

        var refund = new Transaction(
            source.Id, TransactionType.Credit, TransactionCategory.Transfer, amount,
            "Reversal: transfer failed", null);

        const string insertTransactionSql = """
            INSERT INTO Transactions
            (Id, AccountId, TransactionType, TransactionCategory, Amount, Description, CreatedAt,
             ClientReference)
            VALUES
            (@Id, @AccountId, @TransactionType, @TransactionCategory, @Amount, @Description, @CreatedAt,
             @ClientReference);
            """;

        await connection.ExecuteAsync(insertTransactionSql, refund, dbTransaction);

        dbTransaction.Commit();
    }
}
