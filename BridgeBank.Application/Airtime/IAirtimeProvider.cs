using BridgeBank.Application.Airtime.DTOs;
using BridgeBank.Application.Airtime.Models;

namespace BridgeBank.Application.Airtime
{
    public interface IAirtimeProvider
    {
        Task<AirtimePurchaseResult> PurchaseAsync(AirtimePurchaseRequest request, CancellationToken cancellationToken = default);

        string GenerateRequestId();
        Task<AirtimePurchaseResult> RequeryAsync(string requestId, CancellationToken cancellationToken = default);

    }    
        
}
