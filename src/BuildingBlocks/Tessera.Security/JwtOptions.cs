using System.ComponentModel.DataAnnotations;

namespace Tessera.Security;

/// <summary>
/// Token settings shared by the issuer (Wallet.Service) and every resource server
/// (Gateway, Wallet.Service, Scoring.Service).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; init; } = "tessera-identity";

    [Required]
    public string Audience { get; init; } = "tessera-clients";

    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; init; } = 60;
}
