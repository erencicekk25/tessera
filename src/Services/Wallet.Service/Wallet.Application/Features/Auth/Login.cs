using FluentValidation;
using Tessera.SharedKernel.Results;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;
using Wallet.Domain.Abstractions;
using Wallet.Domain.Users;

namespace Wallet.Application.Features.Auth;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthResponse>;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty();
        RuleFor(c => c.Password).NotEmpty();
    }
}

internal sealed class LoginCommandHandler(
    IUserAccountRepository users,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(command.Email);
        if (email.IsFailure)
        {
            // Never reveal which part of the credentials was wrong.
            return UserErrors.InvalidCredentials;
        }

        var user = await users.GetByEmailAsync(email.Value, cancellationToken);
        if (user is null || !passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            return UserErrors.InvalidCredentials;
        }

        user.RecordLogin(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var token = tokenIssuer.Issue(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc, new UserDto(user.Id, user.Email.Value, user.FullName));
    }
}
