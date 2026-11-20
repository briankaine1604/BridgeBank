using BridgeBank.Application.Airtime;
using BridgeBank.Application.Airtime.DTOs;
using BridgeBank.Application.Airtime.Models;
using BridgeBank.Application.Interbank;
using BridgeBank.Domain.Enums;
using System.Net.Http.Json;

namespace BridgeBank.Infrastructure.Interbank;

// This bank holds no VTpass credentials and never calls VTpass: every purchase is a call to the
// other bank's interbank endpoint, which buys it and answers with the purchase's status.
public class PeerAirtimeProvider : IAirtimeProvider
{
    private readonly HttpClient _httpClient;
    private readonly BankOptions _bank;

    public PeerAirtimeProvider(HttpClient httpClient, BankOptions bank)
    {
        _httpClient = httpClient;
        _bank = bank;
    }

    // Only the other bank sees this id, so it needs no date prefix.
    public string GenerateRequestId() => Guid.NewGuid().ToString();

    public async Task<AirtimePurchaseResult> PurchaseAsync(
        AirtimePurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new PeerAirtimeRequest(
            _bank.Code, request.RequestId, request.Network, request.PhoneNumber, request.Amount);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/interbank/airtime", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new AirtimePurchaseResult(
                    AirtimePurchaseOutcome.Unknown,
                    Message: $"The other bank answered {(int)response.StatusCode}.");

            var answer = await response.Content.ReadFromJsonAsync<PeerAirtimeResponse>(cancellationToken);

            var outcome = answer?.Status switch
            {
                AirtimePurchaseStatus.Success => AirtimePurchaseOutcome.Success,
                AirtimePurchaseStatus.Failed => AirtimePurchaseOutcome.Failed,
                AirtimePurchaseStatus.Processing => AirtimePurchaseOutcome.Processing,
                _ => AirtimePurchaseOutcome.Unknown
            };

            return new AirtimePurchaseResult(outcome, answer?.ProviderReference, answer?.ProviderMessage);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // No answer is not a refusal: the row stays Unknown instead of being refunded.
            return new AirtimePurchaseResult(
                AirtimePurchaseOutcome.Unknown, Message: "Could not reach the other bank.");
        }
    }

    // Not exposed by the interbank API yet, so a Processing or Unknown row stays as it is -
    // the same hole VTpass purchases had before requery existed.
    public Task<AirtimePurchaseResult> RequeryAsync(
        string requestId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AirtimePurchaseResult(
            AirtimePurchaseOutcome.Unknown,
            Message: "Requery is not available through the interbank API."));

    private record PeerAirtimeRequest(
        string SourceBankCode,
        string RequestId,
        Network Network,
        string PhoneNumber,
        decimal Amount);

    private record PeerAirtimeResponse(
        AirtimePurchaseStatus Status,
        string? ProviderReference,
        string? ProviderMessage);
}
