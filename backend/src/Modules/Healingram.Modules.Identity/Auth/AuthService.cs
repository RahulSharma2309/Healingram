using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Data;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Identity.Auth;

internal sealed record RegisterRequest(string? Email, string? Password, string? FullName, string? Role);
internal sealed record LoginRequest(string? Email, string? Password);
internal sealed record RefreshRequest(string? RefreshToken);
internal sealed record LogoutRequest(string? RefreshToken);
internal sealed record AuthUserResponse(Guid Id, string Email, string? FullName, string Role);
internal sealed record TokenResponse(string AccessToken, string RefreshToken, AuthUserResponse User);

internal enum AuthStatus
{
    Ok,
    Created,
    NoContent,
    Validation,
    DuplicateEmail,
    Unauthorized
}

internal sealed record AuthResult(
    AuthStatus Status,
    TokenResponse? Tokens = null,
    AuthUserResponse? User = null,
    string? Error = null,
    IReadOnlyList<string>? Details = null)
{
    public static AuthResult Ok(TokenResponse tokens) => new(AuthStatus.Ok, Tokens: tokens);
    public static AuthResult Created(TokenResponse tokens) => new(AuthStatus.Created, Tokens: tokens);
    public static AuthResult CurrentUser(AuthUserResponse user) => new(AuthStatus.Ok, User: user);
    public static AuthResult LoggedOut() => new(AuthStatus.NoContent);
    public static AuthResult Invalid(params string[] details)
        => new(AuthStatus.Validation, Error: "Validation failed", Details: details);
    public static AuthResult Duplicate()
        => new(AuthStatus.DuplicateEmail, Error: "Email already registered", Details: []);
    public static AuthResult Rejected(string error)
        => new(AuthStatus.Unauthorized, Error: error, Details: []);
}

internal sealed class AuthService(
    IIdentityStore store,
    IUserPasswordHasher passwords,
    ITokenService tokens,
    ILogger<AuthService> logger)
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var details = ValidateCredentials(request.Email, request.Password, requireNewPassword: true, out var email);
        if (details.Count > 0)
        {
            return AuthResult.Invalid([.. details]);
        }

        if (await store.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return AuthResult.Duplicate();
        }

        var fullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
        var user = new IdentityUser(Guid.NewGuid(), email, fullName, Roles.Customer, "active");

        try
        {
            user = await store.CreateUserAsync(user, passwords.Hash(request.Password!), cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return AuthResult.Duplicate();
        }

        logger.LogInformation("Registered user {UserId}", user.Id);
        return AuthResult.Created(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var details = ValidateCredentials(request.Email, request.Password, requireNewPassword: false, out var email);
        if (details.Count > 0)
        {
            return AuthResult.Invalid([.. details]);
        }

        var user = await store.FindByEmailAsync(email, cancellationToken);
        var hash = user is null ? null : await store.GetPasswordHashAsync(user.Id, cancellationToken);
        if (user is null || user.Status != "active" || hash is null || !passwords.Verify(hash, request.Password!))
        {
            return AuthResult.Rejected("Invalid credentials");
        }

        logger.LogInformation("User {UserId} signed in", user.Id);
        return AuthResult.Ok(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<AuthResult> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthResult.Invalid("refreshToken is required");
        }

        var stored = await store.FindActiveRefreshTokenAsync(tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (stored is null)
        {
            return AuthResult.Rejected("Invalid refresh token");
        }

        var user = await store.FindByIdAsync(stored.UserId, cancellationToken);
        if (user is null || user.Status != "active")
        {
            return AuthResult.Rejected("Invalid refresh token");
        }

        await store.RevokeRefreshTokenAsync(stored.Id, cancellationToken);
        return AuthResult.Ok(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<AuthResult> LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthResult.Invalid("refreshToken is required");
        }

        var stored = await store.FindActiveRefreshTokenAsync(tokens.HashRefreshToken(request.RefreshToken), cancellationToken);
        if (stored is not null)
        {
            await store.RevokeRefreshTokenAsync(stored.Id, cancellationToken);
            logger.LogInformation("Refresh token revoked for user {UserId}", stored.UserId);
        }

        return AuthResult.LoggedOut();
    }

    public async Task<AuthResult> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindByIdAsync(userId, cancellationToken);
        if (user is null || user.Status != "active")
        {
            return AuthResult.Rejected("Unauthorized");
        }

        return AuthResult.CurrentUser(ToResponse(user));
    }

    private async Task<TokenResponse> IssueTokensAsync(IdentityUser user, CancellationToken cancellationToken)
    {
        var refresh = tokens.CreateRefreshToken();
        await store.StoreRefreshTokenAsync(Guid.NewGuid(), user.Id, refresh.Hash, refresh.ExpiresAt, cancellationToken);
        return new TokenResponse(tokens.CreateAccessToken(user), refresh.Token, ToResponse(user));
    }

    private static AuthUserResponse ToResponse(IdentityUser user)
        => new(user.Id, user.Email, user.FullName, user.Role);

    private static List<string> ValidateCredentials(string? email, string? password, bool requireNewPassword, out string normalizedEmail)
    {
        var details = new List<string>();
        normalizedEmail = email?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(normalizedEmail) || !normalizedEmail.Contains('@'))
        {
            details.Add("email is required");
        }

        if (string.IsNullOrEmpty(password))
        {
            details.Add(requireNewPassword ? "password must be at least 8 characters" : "password is required");
        }
        else if (requireNewPassword && password.Length < 8)
        {
            details.Add("password must be at least 8 characters");
        }

        return details;
    }
}
