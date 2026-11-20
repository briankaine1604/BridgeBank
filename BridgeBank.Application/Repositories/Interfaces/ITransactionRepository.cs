using BridgeBank.Application.Transactions.Models;

namespace BridgeBank.Application.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<TransactionPage?> GetPageByAccountIdAsync(Guid accountId, Guid customerId, int page, int pageSize);
}
