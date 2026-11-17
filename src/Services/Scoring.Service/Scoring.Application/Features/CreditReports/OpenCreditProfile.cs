using Scoring.Application.Abstractions;
using Scoring.Domain.Abstractions;
using Scoring.Domain.CreditProfiles;
using Tessera.Application.Messaging;
using Tessera.SharedKernel.Results;

namespace Scoring.Application.Features.CreditReports;

public sealed record OpenCreditProfileCommand(Guid UserId, string DisplayName, DateTime OccurredAtUtc) : ICommand<CreditReportDto>;

/// <summary>Idempotent: a duplicate or late registration event only refreshes the display name.</summary>
internal sealed class OpenCreditProfileCommandHandler(ICreditProfileRepository profiles, IUnitOfWork unitOfWork)
    : ICommandHandler<OpenCreditProfileCommand, CreditReportDto>
{
    public async Task<Result<CreditReportDto>> Handle(OpenCreditProfileCommand command, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetAsync(command.UserId, cancellationToken);

        if (profile is null)
        {
            profile = CreditProfile.Open(command.UserId, command.DisplayName, command.OccurredAtUtc);
            profiles.Add(profile);
        }
        else
        {
            profile.Rename(command.DisplayName);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return profile.ToReport();
    }
}
