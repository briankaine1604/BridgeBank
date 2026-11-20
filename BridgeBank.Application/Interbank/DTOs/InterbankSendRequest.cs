namespace BridgeBank.Application.Interbank.DTOs;

public record InterbankSendRequest(
    string DestinationAccountNumber,
    decimal Amount,
    string SourceBankCode,
    string SourceAccountNumber,
    string ClientReference);
