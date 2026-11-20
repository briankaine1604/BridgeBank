using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;
using BridgeBank.Application.Repositories.Interfaces;
using BridgeBank.Application.Services.Interfaces;

namespace BridgeBank.Application.Services;

public class InterbankService : IInterbankService
{
    private readonly IInterbankCreditRepository _interbankCreditRepository;

    public InterbankService(IInterbankCreditRepository interbankCreditRepository)
    {
        _interbankCreditRepository = interbankCreditRepository;
    }

    public Task<InterbankCreditResult> CreditAsync(InterbankCreditRequest request)
    {
        if (request.Amount <= 0
            || string.IsNullOrWhiteSpace(request.AccountNumber)
            || string.IsNullOrWhiteSpace(request.SourceBankCode)
            || string.IsNullOrWhiteSpace(request.SourceAccountNumber))
            return Task.FromResult(new InterbankCreditResult(InterbankCreditOutcome.InvalidRequest));

        var clientReference = request.ClientReference?.Trim() ?? string.Empty;

        if (clientReference.Length == 0 || clientReference.Length > 40)
            return Task.FromResult(new InterbankCreditResult(InterbankCreditOutcome.InvalidRequest));

        return _interbankCreditRepository.CreditAsync(
            request.AccountNumber.Trim(),
            request.SourceBankCode.Trim(),
            request.SourceAccountNumber.Trim(),
            clientReference,
            request.Amount);
    }
}
