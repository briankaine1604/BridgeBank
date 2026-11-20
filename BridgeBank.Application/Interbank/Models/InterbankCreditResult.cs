namespace BridgeBank.Application.Interbank.Models;

public enum InterbankCreditOutcome
{
    Success,
    InvalidRequest,
    UnknownSender,
    AccountNotFound,
    AccountInactive
}

public record InterbankCreditResult(InterbankCreditOutcome Outcome, decimal Balance = 0);
