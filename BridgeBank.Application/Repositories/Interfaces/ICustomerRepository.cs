using BridgeBank.Domain.Entities;

namespace BridgeBank.Application.Repositories.Interfaces;

public interface ICustomerRepository
{
    Task<bool> TryCreateAsync(Customer customer);

    Task<Customer?> GetByIdAsync(Guid id);

    Task<Customer?> GetByEmailAsync(string email);
}