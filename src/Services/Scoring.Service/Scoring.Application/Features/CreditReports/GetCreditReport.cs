using Scoring.Domain.Abstractions;
using Scoring.Domain.CreditProfiles;
using Tessera.Application.Messaging;
using Tessera.SharedKernel.Results;

namespace Scoring.Application.Features.CreditReports;

public sealed record GetCreditReportQuery(Guid UserId, string? DisplayName) : IQuery<CreditReportDto>;

internal sealed class GetCreditReportQueryHandler(ICreditProfileRepository profiles, TimeProvider clock)
    : IQueryHandler<GetCreditReportQuery, CreditReportDto>
{
    public async Task<Result<CreditReportDto>> Handle(GetCreditReportQuery query, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetAsync(query.UserId, cancellationToken);

        // The UserRegistered event may still be in flight right after sign-up:
        // answer with a provisional (unsaved) file instead of a 404.
        profile ??= CreditProfile.Open(query.UserId, query.DisplayName, clock.GetUtcNow().UtcDateTime);

        return profile.ToReport();
    }
}
