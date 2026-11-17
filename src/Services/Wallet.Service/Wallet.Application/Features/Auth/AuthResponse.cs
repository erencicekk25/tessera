namespace Wallet.Application.Features.Auth;

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserDto User);

public sealed record UserDto(Guid Id, string Email, string FullName);
