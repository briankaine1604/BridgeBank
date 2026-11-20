using BridgeBank.Application.Customers.DTOs;

namespace BridgeBank.Application.Customers.Models;

public enum RegistrationOutcome
{
    Success,
    InvalidField,
    InvalidEmail,
    InvalidPassword,
    EmailAlreadyRegistered
}

public record RegistrationResult(
    RegistrationOutcome Outcome,
    CustomerResponse? Customer = null
);
