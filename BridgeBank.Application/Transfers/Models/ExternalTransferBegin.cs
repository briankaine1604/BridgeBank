using BridgeBank.Application.Transfers.Models;
using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Transfers.Models;

// ExistingStatus is set when this client reference was already used: the caller replays instead
// of debiting and sending again.
public record ExternalTransferBegin(
    TransferOutcome Outcome,
    Guid TransferId = default,
    string SourceAccountNumber = "",
    decimal SourceBalance = 0,
    TransferStatus? ExistingStatus = null);
