using BridgeBank.Application.Customers.DTOs;
using BridgeBank.Application.Customers.Models;

namespace BridgeBank.Application.Services.Interfaces;

public interface ICustomerService
{
    Task<RegistrationResult> CreateAsync(RegisterCustomerRequest request);

    Task<CustomerResponse?> GetByIdAsync(Guid id);
}
