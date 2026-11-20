using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Airtime.DTOs;

public record BuyAirtimeRequest(Network Network, string PhoneNumber, decimal Amount, string ClientReference);