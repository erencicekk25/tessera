using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Scoring.Application.Abstractions;
using Scoring.Application.Features.CreditReports;
using Tessera.Security;

namespace Scoring.Api.Realtime;

/// <summary>
/// Server-to-client channel. Clients never send commands through the hub; they only listen for
/// <see cref="ScoreHubEvents.ScoreUpdated"/>. Each connection is addressed by its JWT subject.
/// </summary>
[Authorize]
public sealed class ScoreHub : Hub
{
    public const string Route = "/hubs/score";
}

public static class ScoreHubEvents
{
    public const string ScoreUpdated = "scoreUpdated";
}

/// <summary>Routes SignalR "users" by the <c>sub</c> claim instead of ClaimTypes.NameIdentifier.</summary>
public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => connection.User.FindUserId()?.ToString();
}

internal sealed class SignalRScoreNotifier(IHubContext<ScoreHub> hub, ILogger<SignalRScoreNotifier> logger) : IScoreNotifier
{
    public async Task PublishAsync(Guid userId, CreditReportDto report, CancellationToken cancellationToken = default)
    {
        try
        {
            await hub.Clients.User(userId.ToString()).SendAsync(ScoreHubEvents.ScoreUpdated, report, cancellationToken);
        }
        catch (Exception ex)
        {
            // Real-time push is best effort: the score is already committed and can be fetched over REST.
            logger.LogWarning(ex, "Could not push score update to {UserId}", userId);
        }
    }
}
