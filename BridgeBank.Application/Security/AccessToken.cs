namespace BridgeBank.Application.Security;

public record AccessToken(
    string Value,
    DateTime ExpiresAt
);
