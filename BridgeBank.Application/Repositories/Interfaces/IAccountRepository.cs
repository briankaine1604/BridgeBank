using System.Data;
using BridgeBank.Application.Accounts.Models;
using BridgeBank.Domain.Entities;
using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Repositories.Interfaces;

public interface IAccountRepository
{
    Task CreateAsync(Account account);
    Task<Account?> GetByIdAsync(Guid id, Guid customerId);
    Task<IEnumerable<Account>> GetByCustomerIdAsync(Guid customerId);
    Task<long> GetNextAccountNumberAsync();

    Task<AccountMovementResult> ApplyMovementAsync(
        Guid accountId,
        Guid customerId,
        decimal amount,
        TransactionType transactionType,
        TransactionCategory transactionCategory,
        string? description);
}
