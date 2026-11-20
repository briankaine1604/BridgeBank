using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Transactions.DTOs;

public record CreateTransactionRequest(
    Guid AccountId,
    decimal Amount,
    TransactionCategory Category,
    string? Description
);
