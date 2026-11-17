using FluentValidation;
using Tessera.SharedKernel.Results;
using Tessera.Application.Messaging;
using Wallet.Application.Abstractions;
using Wallet.Domain.Abstractions;
using Wallet.Domain.Users;
using Wallet.Domain.Wallets;

namespace Wallet.Application.Features.Auth;

public sealed record RegisterUserCommand(string Email, string FullName, string Password) : ICommand<AuthResponse>;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(c => c.FullName).NotEmpty().MinimumLength(2).MaximumLength(80);
        RuleFor(c => c.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

/// <summary>
/// Signs a customer up and opens their wallet in the same transaction. The resulting
/// <see cref="UserRegisteredDomainEvent"/> is relayed to Scoring.Service through the outbox.
/// </summary>
internal sealed class RegisterUserCommandHandler(
    IUserAccountRepository users,
    IWalletRepository wallets,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<Result<AuthResponse>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(command.Email);
        if (email.IsFailure)
        {
            return email.Error;
        }

        if (await users.ExistsAsync(email.Value, cancellationToken))
        {
            return UserErrors.EmailTaken;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var registration = UserAccount.Register(email.Value, command.FullName, passwordHasher.Hash(command.Password), now);
        if (registration.IsFailure)
        {
            return registration.Error;
        }

        var user = registration.Value;
        users.Add(user);
        wallets.Add(DigitalWallet.Open(user.Id, now));

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var token = tokenIssuer.Issue(user);
        return new AuthResponse(token.Value, token.ExpiresAtUtc, new UserDto(user.Id, user.Email.Value, user.FullName));
    }
}
