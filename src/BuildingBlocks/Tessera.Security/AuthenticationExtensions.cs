using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Tessera.Security;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Registers symmetric-key JWT bearer validation with the Tessera conventions.
    /// </summary>
    /// <param name="hubPathPrefix">
    /// When set, access tokens are also accepted from the <c>access_token</c> query string for
    /// requests under this path. Browsers cannot attach headers to WebSocket upgrades, so SignalR relies on it.
    /// </param>
    public static IServiceCollection AddTesseraJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string? hubPathPrefix = null)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = TesseraClaimTypes.Name
                };

                if (hubPathPrefix is not null)
                {
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Query["access_token"];
                            if (!string.IsNullOrEmpty(token) &&
                                context.HttpContext.Request.Path.StartsWithSegments(hubPathPrefix))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        }
                    };
                }
            });

        services.AddAuthorization();
        return services;
    }
}
