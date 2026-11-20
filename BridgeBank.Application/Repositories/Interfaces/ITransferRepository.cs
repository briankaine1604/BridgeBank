using BridgeBank.Application.Transfers.Models;

namespace BridgeBank.Application.Repositories.Interfaces;

public interface ITransferRepository
{
    Task<TransferResult> TransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        string destinationAccountNumber,
        decimal amount,
        string? description,
        string clientReference);

    Task<ExternalTransferBegin> BeginExternalTransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        string destinationAccountNumber,
        string destinationBankCode,
        decimal amount,
        string? description,
        string clientReference);

    Task CompleteExternalTransferAsync(Guid transferId);

    Task FailExternalTransferAsync(Guid transferId);
}
