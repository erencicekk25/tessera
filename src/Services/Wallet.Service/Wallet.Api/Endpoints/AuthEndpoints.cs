using MediatR;
using Tessera.AspNetCore;
using Wallet.Application.Features.Auth;

namespace Wallet.Api.Endpoints;

public static class AuthEndpoints
{
    public sealed record RegisterRequest(string Email, string FullName, string Password);

    public sealed record LoginRequest(string Email, string Password);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Authentication").AllowAnonymous();

        group.MapPost("/register", async (RegisterRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new RegisterUserCommand(request.Email, request.FullName, request.Password), ct))
                .ToHttpResult(response => Results.Created("/api/wallet", response)))
            .WithName("Register")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", async (LoginRequest request, ISender sender, CancellationToken ct) =>
                (await sender.Send(new LoginCommand(request.Email, request.Password), ct)).ToHttpResult())
            .WithName("Login")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
