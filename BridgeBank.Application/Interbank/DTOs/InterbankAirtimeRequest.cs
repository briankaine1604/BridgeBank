using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Interbank.DTOs;

// RequestId is the asking bank's own id for this purchase, unique per bank, so it is what a
// retry is deduped on. It never reaches VTpass: this bank generates its own.
public record InterbankAirtimeRequest(
    string SourceBankCode,
    string RequestId,
    Network Network,
    string PhoneNumber,
    decimal Amount);
