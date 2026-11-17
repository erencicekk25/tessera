using System.Text.RegularExpressions;
using Tessera.SharedKernel.Results;

namespace Wallet.Domain.Users;

/// <summary>Normalised, validated e-mail address value object.</summary>
public sealed partial record EmailAddress
{
    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static Result<EmailAddress> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return UserErrors.EmailRequired;
        }

        var normalised = raw.Trim().ToLowerInvariant();
        if (normalised.Length > 254 || !EmailPattern().IsMatch(normalised))
        {
            return UserErrors.EmailInvalid;
        }

        return new EmailAddress(normalised);
    }

    /// <summary>Rehydrates an already validated value (persistence only).</summary>
    public static EmailAddress FromTrusted(string value) => new(value);

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}
