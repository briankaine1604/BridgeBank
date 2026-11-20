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

    public InterbankController(IInterbankService interbankService)
    {
        _interbankService = interbankService;
    }

    // No JWT here: InterbankCallerMiddleware authenticates the calling bank by IP and shared secret.
    [HttpPost("credits")]
    public async Task<IActionResult> Credit([FromBody] InterbankCreditRequest request)
    {
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
}
