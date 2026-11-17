using MassTransit;
using MediatR;
using Scoring.Application.Features.CreditReports;
using Scoring.Domain.CreditProfiles;
using Tessera.Contracts;

namespace Scoring.Infrastructure.Messaging;

/// <summary>
/// Thin adapters: translate the Wallet context's published language into Scoring commands.
/// No business logic lives here.
/// </summary>
public sealed class UserRegisteredConsumer(ISender sender) : IConsumer<UserRegisteredIntegrationEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredIntegrationEvent> context)
    {
        var message = context.Message;
        await sender.Send(new OpenCreditProfileCommand(message.UserId, message.FullName, message.OccurredAtUtc), context.CancellationToken);
    }
}

public sealed class TransactionCompletedConsumer(ISender sender) : IConsumer<TransactionCompletedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TransactionCompletedIntegrationEvent> context)
    {
        var message = context.Message;
        var kind = message.Kind switch
        {
            TransactionKind.Deposit => SignalKind.Deposit,
            TransactionKind.Withdrawal => SignalKind.Withdrawal,
            TransactionKind.TransferIn => SignalKind.TransferIn,
            TransactionKind.TransferOut => SignalKind.TransferOut,
            _ => throw new ArgumentOutOfRangeException(nameof(context), message.Kind, "Unknown transaction kind.")
        };

        await sender.Send(
            new RecordTransactionSignalCommand(message.UserId, kind, message.Amount, message.BalanceAfter, message.OccurredAtUtc),
            context.CancellationToken);
    }
}

public sealed class TransactionDeclinedConsumer(ISender sender) : IConsumer<TransactionDeclinedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TransactionDeclinedIntegrationEvent> context)
    {
        var message = context.Message;
        await sender.Send(
            new RecordTransactionSignalCommand(message.UserId, SignalKind.Declined, message.Amount, message.CurrentBalance, message.OccurredAtUtc),
            context.CancellationToken);
    }
}
