using Scoring.Domain.CreditProfiles;

namespace Scoring.Domain.Abstractions;

public interface ICreditProfileRepository
{
    Task<CreditProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);

    void Add(CreditProfile profile);
}
