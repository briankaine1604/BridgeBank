using BridgeBank.Application.Interbank.Models;
using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Repositories.Interfaces;

public interface IInterbankAirtimeRepository
{
    Task<InterbankAirtimeRecord?> FindAsync(string sourceBankCode, string requestId);

    // Returns Guid.Empty when another call already inserted this (bank, requestId).
    Task<Guid> InsertPendingAsync(
        string sourceBankCode,
        string requestId,
        Network network,
        string phoneNumber,
        decimal amount,
        string providerRequestId);

    Task SettleAsync(Guid id, AirtimePurchaseStatus status, string? providerReference, string? providerMessage);
}
