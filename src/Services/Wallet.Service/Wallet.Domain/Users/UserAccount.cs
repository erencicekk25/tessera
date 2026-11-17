using Tessera.SharedKernel.Domain;
using Tessera.SharedKernel.Results;

namespace Wallet.Domain.Users;

/// <summary>
/// The customer identity. Owns credentials; the money lives in a separate <c>DigitalWallet</c>
/// aggregate so the two can evolve and be locked independently.
/// </summary>
public sealed class UserAccount : AggregateRoot<Guid>
{
    private UserAccount(Guid id, EmailAddress email, string fullName, string passwordHash, DateTime registeredAtUtc)
        : base(id)
    {
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        RegisteredAtUtc = registeredAtUtc;
    }

    private UserAccount() { }

    public EmailAddress Email { get; private set; } = null!;

    public string FullName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime RegisteredAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public static Result<UserAccount> Register(EmailAddress email, string fullName, string passwordHash, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return UserErrors.FullNameRequired;
        }

        var user = new UserAccount(Guid.NewGuid(), email, fullName.Trim(), passwordHash, nowUtc);
        user.Raise(new UserRegisteredDomainEvent(user.Id, user.FullName, nowUtc));
        return user;
    }

    public void RecordLogin(DateTime nowUtc) => LastLoginAtUtc = nowUtc;
}

public sealed record UserRegisteredDomainEvent(Guid UserId, string FullName, DateTime OccurredAtUtc)
    : DomainEvent(OccurredAtUtc);
