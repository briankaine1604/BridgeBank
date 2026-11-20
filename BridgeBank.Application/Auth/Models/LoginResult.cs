using BridgeBank.Application.Auth.DTOs;

namespace BridgeBank.Application.Auth.Models;

public enum LoginOutcome
{
    Success,
    InvalidCredentials
}

public record LoginResult(
    LoginOutcome Outcome,
    LoginResponse? Response = null
);
