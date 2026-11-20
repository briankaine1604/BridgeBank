using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;

namespace BridgeBank.Application.Interbank;

public interface IInterbankClient
{
    Task<InterbankSendResult> SendAsync(
        InterbankSendRequest request,
        CancellationToken cancellationToken = default);
}
