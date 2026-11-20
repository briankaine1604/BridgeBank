using System.Text.RegularExpressions;
using BridgeBank.Application.Airtime;
using BridgeBank.Application.Airtime.DTOs;
using BridgeBank.Application.Airtime.Models;
using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;
using BridgeBank.Application.Repositories.Interfaces;
using BridgeBank.Application.Services.Interfaces;
using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Services;

// The other bank has no provider credentials, so it asks this one to buy. The purchase itself is
// an ordinary VTpass call; what is new is the (bank, requestId) record that stops a retry buying twice.
public class InterbankAirtimeService : IInterbankAirtimeService
{
    private static readonly Regex PhoneFormat = new(@"^0\d{10}$");

    private readonly IInterbankAirtimeRepository _repository;
    private readonly IAirtimeProvider _provider;

    public InterbankAirtimeService(IInterbankAirtimeRepository repository, IAirtimeProvider provider)
    {
        _repository = repository;
        _provider = provider;
    }

    public async Task<InterbankAirtimeResult> BuyAsync(InterbankAirtimeRequest request)
    {
        if (request.Amount <= 0
            || !Enum.IsDefined(request.Network)
            || !PhoneFormat.IsMatch(request.PhoneNumber ?? string.Empty))
            return new InterbankAirtimeResult(InterbankAirtimeOutcome.InvalidRequest);

        var sourceBankCode = request.SourceBankCode?.Trim() ?? string.Empty;
        var requestId = request.RequestId?.Trim() ?? string.Empty;

        if (sourceBankCode.Length == 0 || requestId.Length == 0 || requestId.Length > 40)
            return new InterbankAirtimeResult(InterbankAirtimeOutcome.InvalidRequest);

        var existing = await _repository.FindAsync(sourceBankCode, requestId);
        if (existing is not null)
            return ToResult(existing);

        var providerRequestId = _provider.GenerateRequestId();

        var id = await _repository.InsertPendingAsync(
            sourceBankCode, requestId, request.Network, request.PhoneNumber, request.Amount,
            providerRequestId);

        // Lost the race to a concurrent call with the same key: answer with what that one recorded.
        if (id == Guid.Empty)
        {
            var raced = await _repository.FindAsync(sourceBankCode, requestId);
            return raced is null
                ? new InterbankAirtimeResult(InterbankAirtimeOutcome.InvalidRequest)
                : ToResult(raced);
        }

        var result = await _provider.PurchaseAsync(
            new AirtimePurchaseRequest(providerRequestId, request.Network, request.PhoneNumber, request.Amount),
            CancellationToken.None);

        var status = result.Outcome switch
        {
            AirtimePurchaseOutcome.Success => AirtimePurchaseStatus.Success,
            AirtimePurchaseOutcome.Failed => AirtimePurchaseStatus.Failed,
            AirtimePurchaseOutcome.Processing => AirtimePurchaseStatus.Processing,
            _ => AirtimePurchaseStatus.Unknown
        };

        await _repository.SettleAsync(id, status, result.ProviderReference, result.Message);

        return new InterbankAirtimeResult(
            InterbankAirtimeOutcome.Success, status, result.ProviderReference, result.Message);
    }

    private static InterbankAirtimeResult ToResult(InterbankAirtimeRecord record)
        => new(
            InterbankAirtimeOutcome.Success,
            record.Status,
            record.ProviderReference,
            record.ProviderMessage);
}
