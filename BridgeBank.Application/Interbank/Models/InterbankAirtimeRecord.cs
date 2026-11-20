using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Interbank.Models;

public record InterbankAirtimeRecord(
    Guid Id,
    AirtimePurchaseStatus Status,
    string? ProviderReference,
    string? ProviderMessage);
