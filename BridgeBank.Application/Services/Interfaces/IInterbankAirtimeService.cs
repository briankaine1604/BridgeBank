using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;

namespace BridgeBank.Application.Services.Interfaces;

public interface IInterbankAirtimeService
{
    Task<InterbankAirtimeResult> BuyAsync(InterbankAirtimeRequest request);
}
