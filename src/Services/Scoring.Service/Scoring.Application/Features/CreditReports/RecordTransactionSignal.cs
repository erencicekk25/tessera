using Microsoft.Extensions.Logging;
using Scoring.Application.Abstractions;
using Scoring.Domain.Abstractions;
using Scoring.Domain.CreditProfiles;
using Tessera.Application.Messaging;
using Tessera.SharedKernel.Results;

namespace Scoring.Application.Features.CreditReports;

public sealed record RecordTransactionSignalCommand(
    Guid UserId,
    SignalKind Kind,
    decimal Amount,
    decimal BalanceAfter,
    DateTime OccurredAtUtc) : ICommand<CreditReportDto>;

/// <summary>
/// Re-scores a credit file after a wallet event and pushes the new report to the customer's browser.
/// Duplicate deliveries are filtered upstream by the MassTransit inbox.
/// </summary>
internal sealed class RecordTransactionSignalCommandHandler(
    ICreditProfileRepository profiles,
    IUnitOfWork unitOfWork,
    IScoreNotifier notifier,
    ILogger<RecordTransactionSignalCommandHandler> logger) : ICommandHandler<RecordTransactionSignalCommand, CreditReportDto>
{
    public async Task<Result<CreditReportDto>> Handle(RecordTransactionSignalCommand command, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetAsync(command.UserId, cancellationToken);
        if (profile is null)
        {
            // Events can overtake each other; open the file on first sight.
            profile = CreditProfile.Open(command.UserId, null, command.OccurredAtUtc);
            profiles.Add(profile);
        }

        var delta = profile.Apply(new TransactionSignal(command.Kind, command.Amount, command.BalanceAfter, command.OccurredAtUtc));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Credit score for {UserId} moved {Delta:+#;-#;0} to {Score} ({Band}) after {Signal}",
            command.UserId, delta, profile.Score, profile.Band, command.Kind);

        var report = profile.ToReport();
        await notifier.PublishAsync(command.UserId, report, cancellationToken);
        return report;
    }
}
