using BridgeBank.Domain.Entities;
using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Airtime.Models;

public enum BuyAirtimeOutcome
{
    Success,          
    InvalidAmount,
    InvalidNetwork,
    InvalidPhoneNumber,
    AccountNotFound,
    AccountInactive,
    InsufficientFunds,
    InvalidClientReference,
    ClientReferenceReused
}

public record BuyAirtimeResult(
    BuyAirtimeOutcome Outcome,
    Guid PurchaseId = default,
    AirtimePurchaseStatus Status = default,
    decimal Balance = 0);

public record BeginAirtimePurchaseResult(
    BuyAirtimeOutcome Outcome,
    AirtimePurchase? Purchase = null,
    decimal Balance = 0,
    bool IsReplay = false);