using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;

namespace BridgeBank.Application.Services.Interfaces;

public interface IInterbankService
{
    Task<InterbankCreditResult> CreditAsync(InterbankCreditRequest request);
}
