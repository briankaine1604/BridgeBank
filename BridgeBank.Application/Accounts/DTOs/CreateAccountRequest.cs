using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Accounts.DTOs;

public record CreateAccountRequest(
    AccountType AccountType
);
