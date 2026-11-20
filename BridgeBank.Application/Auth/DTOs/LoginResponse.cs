namespace BridgeBank.Application.Auth.DTOs;

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt
);
