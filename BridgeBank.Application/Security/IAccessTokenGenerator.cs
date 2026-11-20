using BridgeBank.Domain.Entities;

namespace BridgeBank.Application.Security;

public interface IAccessTokenGenerator
{
    AccessToken Generate(Customer customer);
}
