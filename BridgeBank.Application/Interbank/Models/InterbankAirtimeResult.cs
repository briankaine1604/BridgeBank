using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Interbank.Models;

public enum InterbankAirtimeOutcome
{
    Success,
    InvalidRequest,
    UnknownSender
}

// Status is the purchase's lifecycle at this bank, so the asking bank can settle its own row
// from it. A failed purchase is still a successful answer: the call worked, the provider said no.
public record InterbankAirtimeResult(
    InterbankAirtimeOutcome Outcome,
    AirtimePurchaseStatus Status = default,
    string? ProviderReference = null,
    string? ProviderMessage = null);
