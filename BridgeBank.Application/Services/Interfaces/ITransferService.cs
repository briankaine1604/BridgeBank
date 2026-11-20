using BridgeBank.Application.Transfers.DTOs;
using BridgeBank.Application.Transfers.Models;

namespace BridgeBank.Application.Services.Interfaces;

public interface ITransferService
{
    Task<TransferResult> TransferAsync(Guid sourceAccountId, Guid customerId, TransferRequest request);
}
