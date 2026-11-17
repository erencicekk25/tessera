using System.Security.Claims;

namespace Tessera.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid? FindUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(TesseraClaimTypes.Subject);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public static Guid GetRequiredUserId(this ClaimsPrincipal principal) =>
        principal.FindUserId() ?? throw new UnauthorizedAccessException("The access token does not contain a subject.");
}
