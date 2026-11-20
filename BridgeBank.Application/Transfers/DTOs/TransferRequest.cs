namespace BridgeBank.Application.Transfers.DTOs;

public record TransferRequest(
    string DestinationAccountNumber, 
    decimal Amount, 
    string? Description, 
    string ClientReference,
    string? DestinationBankCode = null);
