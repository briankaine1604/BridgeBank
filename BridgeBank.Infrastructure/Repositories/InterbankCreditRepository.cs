using BridgeBank.Application.Interbank.Models;
using BridgeBank.Application.Repositories.Interfaces;
using BridgeBank.Domain.Entities;
using BridgeBank.Domain.Enums;
using BridgeBank.Infrastructure.Database;
using Dapper;

namespace BridgeBank.Infrastructure.Repositories;

public class InterbankCreditRepository : IInterbankCreditRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public InterbankCreditRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<InterbankCreditResult> CreditAsync(
        string accountNumber,
        string sourceBankCode,
        string sourceAccountNumber,
        string clientReference,
        decimal amount)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var dbTransaction = connection.BeginTransaction();

        // Locked read, then the duplicate check inside the same transaction: a retry of the same
        // key queues behind the first call instead of crediting the account twice.
        const string readSql = """
            SELECT Id, AccountNumber, Balance, AccountType,
                   AccountStatus, CustomerId, CreatedAt
            FROM Accounts WITH (UPDLOCK, ROWLOCK)
            WHERE AccountNumber = @AccountNumber;

            SELECT Id FROM InboundTransfers
            WHERE SourceBankCode = @SourceBankCode AND ClientReference = @ClientReference;
            """;

        using var results = await connection.QueryMultipleAsync(
            readSql,
            new
            {
                AccountNumber = accountNumber,
                SourceBankCode = sourceBankCode,
                ClientReference = clientReference
            },
            dbTransaction);

        var account = await results.ReadSingleOrDefaultAsync<Account>();
        var alreadyCredited = await results.ReadSingleOrDefaultAsync<Guid?>() is not null;

        if (account is null)
        {
            dbTransaction.Rollback();
            return new InterbankCreditResult(InterbankCreditOutcome.AccountNotFound);
        }

        // A replay reports what was already decided, before the status check re-judges it.
        if (alreadyCredited)
        {
            dbTransaction.Rollback();
            return new InterbankCreditResult(InterbankCreditOutcome.Success, account.Balance);
        }

        if (account.AccountStatus != AccountStatus.Active)
        {
            dbTransaction.Rollback();
            return new InterbankCreditResult(InterbankCreditOutcome.AccountInactive);
        }

        account.Deposit(amount);

        const string updateSql = """
            UPDATE Accounts
            SET Balance = @Balance
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(updateSql, new { account.Balance, account.Id }, dbTransaction);

        var transaction = new Transaction(
            account.Id,
            TransactionType.Credit,
            TransactionCategory.Transfer,
            amount,
            $"Transfer from {sourceBankCode} {sourceAccountNumber}",
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

        const string insertInboundSql = """
            INSERT INTO InboundTransfers
            (Id, AccountId, SourceBankCode, SourceAccountNumber, Amount, ClientReference, CreatedAt)
            VALUES
            (@Id, @AccountId, @SourceBankCode, @SourceAccountNumber, @Amount, @ClientReference, @CreatedAt);
            """;

        await connection.ExecuteAsync(
            insertInboundSql,
            new
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                SourceBankCode = sourceBankCode,
                SourceAccountNumber = sourceAccountNumber,
                Amount = amount,
                ClientReference = clientReference,
                CreatedAt = DateTime.UtcNow
            },
            dbTransaction);

        dbTransaction.Commit();

        return new InterbankCreditResult(InterbankCreditOutcome.Success, account.Balance);
    }
}
