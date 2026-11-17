using System.Threading.RateLimiting;
using Tessera.Security;

var builder = WebApplication.CreateBuilder(args);

// The gateway is the single public entry point. It validates JWTs at the edge (fail fast, no
// wasted hops) and the services validate them again — defence in depth, zero implicit trust.
builder.Services.AddTesseraJwtAuthentication(builder.Configuration, hubPathPrefix: "/hubs");
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(GatewayPolicies.Authenticated, policy => policy.RequireAuthenticatedUser());

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];
builder.Services.AddCors(options => options.AddPolicy(GatewayPolicies.Spa, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials() // required by SignalR
    .WithExposedHeaders("Location")));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Brute-force protection for sign-in / sign-up: 10 attempts per minute per client IP.
    options.AddPolicy(GatewayPolicies.AuthThrottle, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors(GatewayPolicies.Spa);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "tessera-gateway", status = "ok" })).AllowAnonymous();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapReverseProxy();

await app.RunAsync();

internal static class GatewayPolicies
{
    public const string Authenticated = "authenticated";
    public const string Spa = "spa";
    public const string AuthThrottle = "auth-throttle";
}
