namespace BridgeBank.Application.Interbank.Models;

public enum InterbankSendOutcome
{
    Accepted,
    Rejected,
    Unavailable
}

// StatusCode is the peer's own answer, so the caller can mirror 404 or 409 instead of guessing.
public record InterbankSendResult(
    InterbankSendOutcome Outcome,
    int StatusCode = 0,
    decimal DestinationBalance = 0);
