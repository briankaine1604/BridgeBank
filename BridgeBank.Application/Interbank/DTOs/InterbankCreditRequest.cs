namespace BridgeBank.Application.Interbank.DTOs;

public record InterbankCreditRequest(
    string AccountNumber,
    decimal Amount,
    string SourceBankCode,
    string SourceAccountNumber,
    string ClientReference);
