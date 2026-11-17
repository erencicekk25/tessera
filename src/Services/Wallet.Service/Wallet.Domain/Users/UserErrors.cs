using Tessera.SharedKernel.Results;

namespace Wallet.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailRequired = Error.Validation("User.EmailRequired", "An e-mail address is required.");
    public static readonly Error EmailInvalid = Error.Validation("User.EmailInvalid", "The e-mail address format is invalid.");
    public static readonly Error EmailTaken = Error.Conflict("User.EmailTaken", "An account with this e-mail address already exists.");
    public static readonly Error InvalidCredentials = Error.Unauthorized("User.InvalidCredentials", "The e-mail or password is incorrect.");
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user could not be found.");
    public static readonly Error FullNameRequired = Error.Validation("User.FullNameRequired", "A full name is required.");
}
