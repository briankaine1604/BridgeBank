using BridgeBank.Application.Interbank;
using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;
using System.Net.Http.Json;

namespace BridgeBank.Infrastructure.Interbank;

public class InterbankClient : IInterbankClient
{
    private readonly HttpClient _httpClient;

    public InterbankClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<InterbankSendResult> SendAsync(
        InterbankSendRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = new InterbankCreditRequest(
            request.DestinationAccountNumber,
            request.Amount,
            request.SourceBankCode,
            request.SourceAccountNumber,
            request.ClientReference);

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/interbank/credits", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new InterbankSendResult(InterbankSendOutcome.Rejected, (int)response.StatusCode);

            var credit = await response.Content.ReadFromJsonAsync<InterbankCreditResponse>(cancellationToken);

            return new InterbankSendResult(
                InterbankSendOutcome.Accepted, (int)response.StatusCode, credit?.Balance ?? 0);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // No answer means no decision: the caller must not treat this as a refusal.
            return new InterbankSendResult(InterbankSendOutcome.Unavailable);
        }
    }

    private record InterbankCreditResponse(string AccountNumber, decimal Balance);
}
