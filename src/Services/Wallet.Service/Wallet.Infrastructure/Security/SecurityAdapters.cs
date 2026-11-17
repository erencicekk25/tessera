using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tessera.Security;
using Wallet.Application.Abstractions;
using Wallet.Domain.Users;
using IPasswordHasher = Wallet.Application.Abstractions.IPasswordHasher;

namespace Wallet.Infrastructure.Security;

/// <summary>Adapter over ASP.NET Core Identity's PBKDF2 hasher (v3 format, per-hash salt).</summary>
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    private static readonly PasswordHasher<object> Hasher = new();
    private static readonly object Subject = new();

    public string Hash(string password) => Hasher.HashPassword(Subject, password);

    public bool Verify(string hashedPassword, string providedPassword) =>
        Hasher.VerifyHashedPassword(Subject, hashedPassword, providedPassword) != PasswordVerificationResult.Failed;
}

internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JwtOptions _options = options.Value;

    public AccessToken Issue(UserAccount user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new[]
        {
            new Claim(TesseraClaimTypes.Subject, user.Id.ToString()),
            new Claim(TesseraClaimTypes.Email, user.Email.Value),
            new Claim(TesseraClaimTypes.Name, user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims, now, expires, credentials);
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
