using BridgeBank.Application.Auth.DTOs;
using BridgeBank.Application.Auth.Models;

namespace BridgeBank.Application.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request);
}
