using BridgeBank.Application.Auth.DTOs;
using BridgeBank.Application.Auth.Models;
using BridgeBank.Application.Customers.DTOs;
using BridgeBank.Application.Customers.Models;
using BridgeBank.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BridgeBank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICustomerService _customerService;

    public AuthController(IAuthService authService, ICustomerService customerService)
    {
        _authService = authService;
        _customerService = customerService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCustomerRequest request)
    {
        var result = await _customerService.CreateAsync(request);

        return result.Outcome switch
        {
            // No Location header: the customer resource is authenticated and self-only.
            RegistrationOutcome.Success => StatusCode(StatusCodes.Status201Created, result.Customer),
            RegistrationOutcome.EmailAlreadyRegistered => Conflict("Email is already registered."),
            RegistrationOutcome.InvalidEmail => BadRequest("Enter a valid email address."),
            RegistrationOutcome.InvalidPassword => BadRequest("Password must be between 8 and 128 characters."),
            RegistrationOutcome.InvalidField => BadRequest(
                "First name and last name must be at most 100 characters, email at most 255 and phone number at most 20."),
            _ => BadRequest("Registration could not be completed.")
        };
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (result.Outcome != LoginOutcome.Success)
            return Unauthorized("Invalid email or password.");

        return Ok(result.Response);
    }
}
