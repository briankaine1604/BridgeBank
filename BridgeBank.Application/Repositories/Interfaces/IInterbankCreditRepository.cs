using BridgeBank.Application.Interbank.Models;

namespace BridgeBank.Application.Repositories.Interfaces;

public interface IInterbankCreditRepository
{
    Task<InterbankCreditResult> CreditAsync(
        string accountNumber,
        string sourceBankCode,
        string sourceAccountNumber,
        string clientReference,
        decimal amount);
}
