using BridgeBank.Application.Interbank;
using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;
using BridgeBank.Application.Repositories.Interfaces;
using BridgeBank.Application.Services.Interfaces;
using BridgeBank.Application.Transfers.DTOs;
using BridgeBank.Application.Transfers.Models;
using BridgeBank.Domain.Enums;

namespace BridgeBank.Application.Services;

public class TransferService : ITransferService
{
    private readonly ITransferRepository _transferRepository;
    private readonly IInterbankClient _interbankClient;
    private readonly BankOptions _bank;
    private readonly InterbankOptions _interbank;

    public TransferService(
        ITransferRepository transferRepository,
        IInterbankClient interbankClient,
        BankOptions bank,
        InterbankOptions interbank)
    {
        _transferRepository = transferRepository;
        _interbankClient = interbankClient;
        _bank = bank;
        _interbank = interbank;
    }

    public async Task<TransferResult> TransferAsync(
        Guid sourceAccountId,
        Guid customerId,
        TransferRequest request)
    {
        if (request.Amount <= 0)
            return new TransferResult(TransferOutcome.InvalidAmount);

        // Trimmed once here so the value stored on insert is the value a retry looks up.
        var clientReference = request.ClientReference?.Trim() ?? string.Empty;

        if (clientReference.Length == 0 || clientReference.Length > 40)
            return new TransferResult(TransferOutcome.InvalidClientReference);

        var destinationBankCode = request.DestinationBankCode?.Trim() ?? string.Empty;

        // No bank code, or our own: the destination account is in this database.
        if (destinationBankCode.Length == 0 || destinationBankCode == _bank.Code)
            return await _transferRepository.TransferAsync(
                sourceAccountId,
                customerId,
                request.DestinationAccountNumber,
                request.Amount,
                request.Description,
                clientReference);

        if (destinationBankCode != _interbank.PeerCode)
            return new TransferResult(TransferOutcome.InvalidDestinationBank);

        return await TransferToPeerAsync(
            sourceAccountId, customerId, request, destinationBankCode, clientReference);
    }

    private async Task<TransferResult> TransferToPeerAsync(
        Guid sourceAccountId,
        Guid customerId,
        TransferRequest request,
        string destinationBankCode,
        string clientReference)
    {
        var begin = await _transferRepository.BeginExternalTransferAsync(
            sourceAccountId,
            customerId,
            request.DestinationAccountNumber,
            destinationBankCode,
            request.Amount,
            request.Description,
            clientReference);

        if (begin.ExistingStatus is not null)
        {
            // Same key, different payment: the stored one is the truth, so this is a conflict.
            if (begin.DestinationAccountNumber != request.DestinationAccountNumber
                || begin.Amount != request.Amount)
                return new TransferResult(TransferOutcome.ClientReferenceReused);

            switch (begin.ExistingStatus)
            {
                case TransferStatus.Completed:
                    return new TransferResult(
                        TransferOutcome.Success, begin.TransferId, begin.SourceBalance);
                case TransferStatus.Failed:
                    return new TransferResult(TransferOutcome.TransferFailed);
            }

            // Still Pending: send again with the stored values. The far bank's own (bank,
            // reference) record is what makes the resend safe rather than a second payment.
        }

        if (begin.Outcome != TransferOutcome.Success)
            return new TransferResult(begin.Outcome);

        var send = await _interbankClient.SendAsync(new InterbankSendRequest(
            request.DestinationAccountNumber,
            request.Amount,
            _bank.Code,
            begin.SourceAccountNumber,
            clientReference));

        if (send.Outcome == InterbankSendOutcome.Accepted)
        {
            await _transferRepository.CompleteExternalTransferAsync(begin.TransferId);

            return new TransferResult(
                TransferOutcome.Success, begin.TransferId, begin.SourceBalance, send.DestinationBalance);
        }

        if (send.Outcome == InterbankSendOutcome.Rejected)
        {
            await _transferRepository.FailExternalTransferAsync(begin.TransferId);

            var outcome = send.StatusCode switch
            {
                404 => TransferOutcome.DestinationAccountNotFound,
                409 => TransferOutcome.AccountInactive,
                _ => TransferOutcome.TransferFailed
            };

            return new TransferResult(outcome);
        }

        // No answer: the money has left but nothing is confirmed, so the transfer stays Pending.
        // A retry with the same client reference replays it instead of sending twice.
        return new TransferResult(TransferOutcome.PeerUnavailable);
    }
}
