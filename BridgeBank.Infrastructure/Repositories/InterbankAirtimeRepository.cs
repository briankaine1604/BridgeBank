using BridgeBank.Application.Interbank.Models;
using BridgeBank.Application.Repositories.Interfaces;
using BridgeBank.Domain.Enums;
using BridgeBank.Infrastructure.Database;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BridgeBank.Infrastructure.Repositories;

public class InterbankAirtimeRepository : IInterbankAirtimeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public InterbankAirtimeRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<InterbankAirtimeRecord?> FindAsync(string sourceBankCode, string requestId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT Id, Status, ProviderReference, ProviderMessage
            FROM InterbankAirtimeRequests
            WHERE SourceBankCode = @SourceBankCode AND RequestId = @RequestId;
            """;

        return await connection.QuerySingleOrDefaultAsync<InterbankAirtimeRecord>(
            sql, new { SourceBankCode = sourceBankCode, RequestId = requestId });
    }

    public async Task<Guid> InsertPendingAsync(
        string sourceBankCode,
        string requestId,
        Network network,
        string phoneNumber,
        decimal amount,
        string providerRequestId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO InterbankAirtimeRequests
            (Id, SourceBankCode, RequestId, Network, PhoneNumber, Amount, ProviderRequestId, Status,
             CreatedAt)
            VALUES
            (@Id, @SourceBankCode, @RequestId, @Network, @PhoneNumber, @Amount, @ProviderRequestId,
             @Status, @CreatedAt);
            """;

        var id = Guid.NewGuid();

        try
        {
            await connection.ExecuteAsync(sql, new
            {
                Id = id,
                SourceBankCode = sourceBankCode,
                RequestId = requestId,
                Network = network,
                PhoneNumber = phoneNumber,
                Amount = amount,
                ProviderRequestId = providerRequestId,
                Status = AirtimePurchaseStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            // Another call with the same key is already in flight or finished.
            return Guid.Empty;
        }

        return id;
    }

    public async Task SettleAsync(
        Guid id,
        AirtimePurchaseStatus status,
        string? providerReference,
        string? providerMessage)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            UPDATE InterbankAirtimeRequests
            SET Status = @Status,
                ProviderReference = @ProviderReference,
                ProviderMessage = @ProviderMessage,
                UpdatedAt = SYSUTCDATETIME()
            WHERE Id = @Id;
            """;

        await connection.ExecuteAsync(sql, new
        {
            Status = status,
            ProviderReference = providerReference,
            ProviderMessage = providerMessage,
            Id = id
        });
    }
}
