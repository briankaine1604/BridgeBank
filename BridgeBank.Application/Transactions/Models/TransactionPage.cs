using BridgeBank.Domain.Entities;

namespace BridgeBank.Application.Transactions.Models;

public record TransactionPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<Transaction> Items)
{
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}
