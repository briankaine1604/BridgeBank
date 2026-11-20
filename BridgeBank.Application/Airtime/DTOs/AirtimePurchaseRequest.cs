using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Airtime.DTOs;

public record AirtimePurchaseRequest(
    string RequestId,
    Network Network,
    string PhoneNumber,
    decimal Amount
);