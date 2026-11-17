using Scoring.Application.Features.CreditReports;

namespace Scoring.Application.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Pushes fresh credit reports to connected clients (implemented with SignalR in the API layer).</summary>
public interface IScoreNotifier
{
    Task PublishAsync(Guid userId, CreditReportDto report, CancellationToken cancellationToken = default);
}
