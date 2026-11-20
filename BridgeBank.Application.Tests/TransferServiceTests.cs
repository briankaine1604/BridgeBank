using BridgeBank.Application.Interbank;
using BridgeBank.Application.Interbank.DTOs;
using BridgeBank.Application.Interbank.Models;
using BridgeBank.Application.Repositories.Interfaces;
using BridgeBank.Application.Services;
using BridgeBank.Application.Transfers.DTOs;
using BridgeBank.Application.Transfers.Models;

namespace BridgeBank.Application.Tests;

public class TransferServiceTests
{
    [Fact]
    public async Task Transfer_PassesRequestThroughToRepository()
    {
        var repository = new FakeTransferRepository();
        var service = new TransferService(
            repository,
            new FakeInterbankClient(),
            new BankOptions { Code = "ANCHOR" },
            new InterbankOptions { PeerCode = "BRDG" });
        var sourceAccountId = Guid.NewGuid();

        await service.TransferAsync(
            sourceAccountId, Guid.NewGuid(),
            new TransferRequest("0000001002", 5000.00m, "Personal transfer", "transfer-ref-1"));

        Assert.Equal(sourceAccountId, repository.SourceAccountId);
        Assert.Equal("0000001002", repository.DestinationAccountNumber);
        Assert.Equal(5000.00m, repository.Amount);
        Assert.Equal("transfer-ref-1", repository.ClientReference);
    }

    private sealed class FakeTransferRepository : ITransferRepository
    {
        public TransferResult Result { get; set; } = new(TransferOutcome.Success);
        public Guid SourceAccountId { get; private set; }
        public string? DestinationAccountNumber { get; private set; }
        public decimal Amount { get; private set; }
        public string? ClientReference { get; private set; }

        public Task<TransferResult> TransferAsync(
            Guid sourceAccountId,
            Guid customerId,
            string destinationAccountNumber,
            decimal amount,
            string? description,
            string clientReference)
        {
            SourceAccountId = sourceAccountId;
            DestinationAccountNumber = destinationAccountNumber;
            Amount = amount;
            ClientReference = clientReference;
            return Task.FromResult(Result);
        }

        public Task<ExternalTransferBegin> BeginExternalTransferAsync(
            Guid sourceAccountId,
            Guid customerId,
            string destinationAccountNumber,
            string destinationBankCode,
            decimal amount,
            string? description,
            string clientReference)
            => throw new InvalidOperationException("Internal transfer must not reach the peer.");

        public Task CompleteExternalTransferAsync(Guid transferId) => Task.CompletedTask;

        public Task FailExternalTransferAsync(Guid transferId) => Task.CompletedTask;
    }

    private sealed class FakeInterbankClient : IInterbankClient
    {
        public Task<InterbankSendResult> SendAsync(
            InterbankSendRequest request,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Internal transfer must not reach the peer.");
    }
}
