using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Data;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Identity.Auth;

internal sealed record RegisterRequest(
    string? Email,
    string? Password,
    string? FullName,
    string? Role,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? ConfirmPassword = null,
    string? Address = null);
internal sealed record UpdateProfileRequest(
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Email,
    string? Address);
internal sealed record LoginRequest(string? Email, string? Password);
internal sealed record RefreshRequest(string? RefreshToken);
internal sealed record LogoutRequest(string? RefreshToken);
internal sealed record GuestVerifyStartRequest(string? Email, string? Phone, string? Channel);
internal sealed record GuestVerifyRequest(string? Email, string? Phone, string? Code);
internal sealed record AuthUserResponse(
    Guid Id,
    string Email,
    string? FullName,
    string Role,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? Address = null,
    string? PhoneCountryCode = null,
    string? AccountStatus = null);
internal sealed record TokenResponse(string AccessToken, string RefreshToken, AuthUserResponse User);

internal enum AuthStatus
{
    Ok,
    Created,
    NoContent,
    Validation,
    DuplicateEmail,
    Unauthorized,
    NoMatch
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
    public static AuthResult ChallengeSent() => new(AuthStatus.Ok);
    public static AuthResult LoggedOut() => new(AuthStatus.NoContent);
    public static AuthResult Invalid(params string[] details)
        => new(AuthStatus.Validation, Error: "Validation failed", Details: details);
    public static AuthResult Duplicate()
        => new(AuthStatus.DuplicateEmail, Error: "Email already registered", Details: []);
    public static AuthResult Rejected(string error)
        => new(AuthStatus.Unauthorized, Error: error, Details: []);
    public static AuthResult NoMatch()
        => new(AuthStatus.NoMatch);
}

internal sealed class AuthService(
    IIdentityStore store,
    IUserPasswordHasher passwords,
    ITokenService tokens,
    ILogger<AuthService> logger)
{
    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var details = ProfileRules.Validate(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.Address,
            request.Password,
            request.ConfirmPassword,
            requirePassword: true);
        if (details.Count > 0)
        {
            return AuthResult.Invalid([.. details]);
        }

        var email = request.Email!.Trim();
        var firstName = request.FirstName!.Trim();
        var lastName = request.LastName!.Trim();
        var fullName = ProfileRules.DisplayName(firstName, lastName, request.FullName, email);
        var phone = ProfileRules.NormalizePhone(request.Phone);
        var existing = await store.FindByEmailAsync(email, cancellationToken);
        if (existing is not null && existing.AccountStatus != AccountStatuses.Guest)
        {
            return AuthResult.Duplicate();
        }

        IdentityUser user;
        if (existing is not null)
        {
            user = existing with
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName,
                PhoneE164 = phone,
                Address = ProfileRules.OptionalAddress(request.Address),
                AccountStatus = AccountStatuses.Registered
            };
            try
            {
                user = await store.PromoteGuestAsync(user, passwords.Hash(request.Password!), cancellationToken);
            }
            catch (DuplicateEmailException)
            {
                return AuthResult.Duplicate();
            }

            logger.LogInformation("Promoted guest {UserId} to registered", user.Id);
            return AuthResult.Created(await IssueTokensAsync(user, cancellationToken));
        }

        user = new IdentityUser(
            Guid.NewGuid(),
            email,
            fullName,
            Roles.Customer,
            "active",
            firstName,
            lastName,
            phone,
            ProfileRules.OptionalAddress(request.Address),
            AccountStatuses.Registered);

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

    public async Task<AuthResult> StartGuestVerificationAsync(
        GuestVerifyStartRequest request,
        CancellationToken cancellationToken)
    {
        var details = ValidateGuestContact(request.Email, request.Phone);
        if (details.Count > 0)
        {
            return AuthResult.Invalid([.. details]);
        }

        var user = await FindGuestContactAsync(request.Email, request.Phone, cancellationToken);
        if (user is not null)
        {
            logger.LogInformation("Guest verification started for user {UserId}", user.Id);
        }
        else
        {
            logger.LogInformation("Guest verification started with no matching customer");
        }

        return AuthResult.ChallengeSent();
    }

    public async Task<AuthResult> VerifyGuestAsync(
        GuestVerifyRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(request.Code?.Trim(), GuestVerification.DevCode, StringComparison.Ordinal))
        {
            return AuthResult.Invalid("verification code is not right");
        }

        var details = ValidateGuestContact(request.Email, request.Phone);
        if (details.Count > 0)
        {
            return AuthResult.Invalid([.. details]);
        }

        var user = await FindGuestContactAsync(request.Email, request.Phone, cancellationToken);
        if (user is null || user.Status != "active")
        {
            return AuthResult.NoMatch();
        }

        logger.LogInformation("Guest verification succeeded for user {UserId}", user.Id);
        return AuthResult.Ok(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<AuthResult> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken)
    {
        var details = ProfileRules.Validate(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Email,
            request.Address);
        if (details.Count > 0)
        {
            return AuthResult.Invalid([.. details]);
        }

        var existing = await store.FindByIdAsync(userId, cancellationToken);
        if (existing is null || existing.Status != "active")
        {
            return AuthResult.Rejected("Unauthorized");
        }

        var email = request.Email!.Trim();
        if (!email.Equals(existing.Email, StringComparison.OrdinalIgnoreCase)
            && await store.FindByEmailAsync(email, cancellationToken) is not null)
        {
            return AuthResult.Duplicate();
        }

        var firstName = request.FirstName!.Trim();
        var lastName = request.LastName!.Trim();
        var updated = existing with
        {
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            FullName = ProfileRules.DisplayName(firstName, lastName, null, email),
            PhoneE164 = ProfileRules.NormalizePhone(request.Phone),
            Address = ProfileRules.OptionalAddress(request.Address)
        };

        try
        {
            updated = await store.UpdateProfileAsync(updated, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return AuthResult.Duplicate();
        }

        logger.LogInformation("Updated profile for user {UserId}", userId);
        return AuthResult.CurrentUser(ToResponse(updated));
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

    private static List<string> ValidateGuestContact(string? email, string? phone)
    {
        var details = new List<string>();
        var normalizedEmail = email?.Trim() ?? "";
        if (normalizedEmail.Length > 0)
        {
            if (!normalizedEmail.Contains('@', StringComparison.Ordinal))
            {
                details.Add("email is not valid");
            }

            return details;
        }

        if (ProfileRules.NormalizePhone(phone) is null)
        {
            details.Add("phone must be exactly 10 digits");
        }

        return details;
    }

    private async Task<IdentityUser?> FindGuestContactAsync(
        string? email,
        string? phone,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email?.Trim() ?? "";
        if (normalizedEmail.Length > 0)
        {
            return await store.FindByEmailAsync(normalizedEmail, cancellationToken);
        }

        var normalizedPhone = ProfileRules.NormalizePhone(phone);
        if (normalizedPhone is null)
        {
            return null;
        }

        return await store.FindByPhoneAsync(normalizedPhone, cancellationToken);
    }

    private static AuthUserResponse ToResponse(IdentityUser user)
    {
        var (firstName, lastName) = ProfileRules.SplitName(user.FirstName, user.LastName, user.FullName);
        return new(
            user.Id,
            user.Email,
            ProfileRules.DisplayName(firstName, lastName, user.FullName, user.Email),
            user.Role,
            firstName,
            lastName,
            user.PhoneE164,
            user.Address,
            user.PhoneE164 is null ? null : ProfileRules.IndiaCountryCode,
            user.AccountStatus);
    }

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
