using BridgeBank.Application.Interbank;
using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;
using BridgeBank.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BridgeBank.Api.Controllers;

[ApiController]
[Route("api/interbank")]
public class InterbankController : ControllerBase
{
    private readonly IInterbankService _interbankService;
    private readonly IInterbankAirtimeService _interbankAirtimeService;
    private readonly InterbankOptions _interbank;

    public InterbankController(
        IInterbankService interbankService,
        IInterbankAirtimeService interbankAirtimeService,
        InterbankOptions interbank)
    {
        _interbankService = interbankService;
        _interbankAirtimeService = interbankAirtimeService;
        _interbank = interbank;
    }

    // No JWT here: InterbankCallerMiddleware authenticates the calling bank by IP and shared secret.
    [HttpPost("credits")]
    public async Task<IActionResult> Credit([FromBody] InterbankCreditRequest request)
    {
        if (request.SourceBankCode != _interbank.PeerCode)
            return BadRequest("Unknown source bank.");

        var result = await _interbankService.CreditAsync(request);

        return result.Outcome switch
        {
            InterbankCreditOutcome.Success => Ok(new
            {
                request.AccountNumber,
                balance = result.Balance
            }),
            InterbankCreditOutcome.AccountNotFound => NotFound("Account not found."),
            InterbankCreditOutcome.AccountInactive => Conflict("Account is not active."),
            InterbankCreditOutcome.UnknownSender => BadRequest("Unknown source bank."),
            _ => BadRequest("Invalid credit request.")
        };
    }

    // Airtime asked for by the other bank: this bank holds the VTpass credentials, so it buys and
    // answers with the purchase's status. A failed purchase is still a 200 - the call worked.
    [HttpPost("airtime")]
    public async Task<IActionResult> Airtime([FromBody] InterbankAirtimeRequest request)
    {
        if (request.SourceBankCode != _interbank.PeerCode)
            return BadRequest("Unknown source bank.");

        var result = await _interbankAirtimeService.BuyAsync(request);

        return result.Outcome switch
        {
            InterbankAirtimeOutcome.Success => Ok(new
            {
                status = result.Status,
                providerReference = result.ProviderReference,
                providerMessage = result.ProviderMessage
            }),
            InterbankAirtimeOutcome.UnknownSender => BadRequest("Unknown source bank."),
            _ => BadRequest("Invalid airtime request.")
        };
    }
}
