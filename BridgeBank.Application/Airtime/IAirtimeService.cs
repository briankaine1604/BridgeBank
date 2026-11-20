using BridgeBank.Application.Airtime.DTOs;
using BridgeBank.Application.Airtime.Models;

namespace BridgeBank.Application.Airtime;

public interface IAirtimeService
{
    Task<BuyAirtimeResult> BuyAsync(Guid accountId, Guid customerId, BuyAirtimeRequest request);
}