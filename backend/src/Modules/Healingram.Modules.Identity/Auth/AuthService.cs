using Healingram.Contracts.Audit;
using Healingram.Contracts.Availability;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Otp;
using Healingram.Contracts.Partners;
using Healingram.Modules.Identity.Auth.Otp;
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
internal sealed record LoginRequest(string? Email, string? Password, string? Portal = null);
internal sealed record RefreshRequest(string? RefreshToken);
internal sealed record LogoutRequest(string? RefreshToken);
internal sealed record GuestVerifyStartRequest(
    string? Email,
    string? Phone,
    string? Channel,
    string? PublicId = null,
    string? Purpose = null);
internal sealed record GuestVerifyRequest(
    string? Email,
    string? Phone,
    string? Code,
    string? PublicId = null,
    string? Purpose = null);
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
    string? AccountStatus = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<PartnerMembershipDto>? PartnerMemberships = null,
    string? AuthKind = null);

internal sealed record PartnerMembershipDto(Guid PartnerId, string PartnerName, string Role, string Status);
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
    IReadOnlyList<string>? Details = null,
    string? DemoCode = null)
{
    public static AuthResult Ok(TokenResponse tokens) => new(AuthStatus.Ok, Tokens: tokens);
    public static AuthResult Created(TokenResponse tokens) => new(AuthStatus.Created, Tokens: tokens);
    public static AuthResult CurrentUser(AuthUserResponse user) => new(AuthStatus.Ok, User: user);
    public static AuthResult ChallengeSent(string? demoCode = null)
        => new(AuthStatus.Ok, DemoCode: demoCode);
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
    IOtpService otp,
    OtpSettings otpSettings,
    ILogger<AuthService> logger,
    IRequestAccessLookup? requestAccess = null,
    IAuditPort? audit = null,
    IPartnerAccess? partners = null)
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

        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? OtpPurposes.RequestAccess : request.Purpose.Trim();
        if (!OtpPurposes.IsKnown(purpose))
        {
            return AuthResult.Invalid("purpose is not valid");
        }

        if (OtpPurposes.IsPrivileged(purpose))
        {
            return AuthResult.Invalid("this verification path cannot grant staff access");
        }

        if (string.Equals(purpose, OtpPurposes.RequestAccess, StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.PublicId))
        {
            return AuthResult.Invalid("publicId is required");
        }

        var destination = DestinationOf(request.Email, request.Phone);
        var user = await FindGuestContactAsync(request.Email, request.Phone, cancellationToken);
        var started = await otp.StartAsync(
            new OtpStartCommand(
                destination,
                request.Channel ?? "email",
                purpose,
                user?.Id,
                request.PublicId?.Trim()),
            cancellationToken);
        if (!started.Sent && started.Error is not null)
        {
            return AuthResult.Invalid(started.Error);
        }

        logger.LogInformation("Guest verification started for purpose {Purpose}", purpose);
        return AuthResult.ChallengeSent(otpSettings.DemoMode ? started.DevelopmentCode : null);
    }

    public async Task<AuthResult> VerifyGuestAsync(
        GuestVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var details = ValidateGuestContact(request.Email, request.Phone);
        if (details.Count > 0)
        {
            return AuthResult.Invalid([.. details]);
        }

        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? OtpPurposes.RequestAccess : request.Purpose.Trim();
        if (OtpPurposes.IsPrivileged(purpose) || purpose != OtpPurposes.RequestAccess)
        {
            return AuthResult.Invalid("this verification path is only for request access");
        }

        if (string.IsNullOrWhiteSpace(request.PublicId))
        {
            return AuthResult.Invalid("publicId is required");
        }

        var destination = DestinationOf(request.Email, request.Phone);
        var verified = await otp.VerifyAsync(
            new OtpVerifyCommand(destination, purpose, request.Code ?? "", request.PublicId?.Trim()),
            cancellationToken);
        if (!verified.Ok)
        {
            return AuthResult.Invalid(verified.Error ?? "verification code is not right");
        }

        var user = await FindGuestContactAsync(request.Email, request.Phone, cancellationToken);
        if (user is null || user.Status != "active")
        {
            return AuthResult.NoMatch();
        }

        string? scopedRequest = request.PublicId?.Trim();
        if (!string.IsNullOrWhiteSpace(scopedRequest) && requestAccess is not null)
        {
            var match = await requestAccess.FindGuestMatchAsync(
                scopedRequest,
                request.Email,
                request.Phone,
                cancellationToken);
            if (match is null || match.CustomerUserId != user.Id)
            {
                return AuthResult.NoMatch();
            }

            scopedRequest = match.PublicId;
        }

        var guestView = user with { Role = Roles.Customer };
        logger.LogInformation("Guest request-access verification succeeded for user {UserId}", user.Id);
        return AuthResult.Ok(await IssueTokensAsync(
            guestView,
            cancellationToken,
            new AccessTokenIssue(OtpPurposes.RequestAccess, scopedRequest, [Roles.Customer], AuthKinds.GuestRequest)));
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
        return AuthResult.CurrentUser(await ToResponseAsync(updated, cancellationToken));
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

        var roles = await store.ListRolesAsync(user.Id, cancellationToken);
        var portalDenied = DenyPortal(request.Portal, user, roles);
        if (portalDenied is not null)
        {
            return AuthResult.Rejected(portalDenied);
        }

        var vendorDenied = await DenyVendorMembershipAsync(request.Portal, roles, user.Id, cancellationToken);
        if (vendorDenied is not null)
        {
            return AuthResult.Rejected(vendorDenied);
        }

        logger.LogInformation("User {UserId} signed in", user.Id);
        if (audit is not null)
        {
            await audit.WriteAsync(
                new AuditEvent(
                    "login",
                    "user",
                    user.Id.ToString(),
                    user.Id,
                    user.Role,
                    System.Diagnostics.Activity.Current?.Id),
                cancellationToken);
        }

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
        AccessTokenIssue? issue = null;
        if (string.Equals(stored.AuthKind, AuthKinds.GuestRequest, StringComparison.OrdinalIgnoreCase)
            || string.Equals(stored.Purpose, OtpPurposes.RequestAccess, StringComparison.OrdinalIgnoreCase))
        {
            issue = new AccessTokenIssue(
                stored.Purpose ?? OtpPurposes.RequestAccess,
                stored.RequestId,
                [Roles.Customer],
                AuthKinds.GuestRequest);
        }

        return AuthResult.Ok(await IssueTokensAsync(user, cancellationToken, issue));
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
            if (audit is not null)
            {
                await audit.WriteAsync(
                    new AuditEvent(
                        "logout",
                        "user",
                        stored.UserId.ToString(),
                        stored.UserId,
                        null,
                        System.Diagnostics.Activity.Current?.Id),
                    cancellationToken);
            }
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

        return AuthResult.CurrentUser(await ToResponseAsync(user, cancellationToken));
    }

    private async Task<TokenResponse> IssueTokensAsync(
        IdentityUser user,
        CancellationToken cancellationToken,
        AccessTokenIssue? issue = null)
    {
        var storedRoles = await store.ListRolesAsync(user.Id, cancellationToken);
        var roles = issue?.Roles is { Count: > 0 } listed
            ? RoleAuthorization.NormalizeRoles(listed)
            : RoleAuthorization.NormalizeRoles(storedRoles, user.Role);
        var issued = issue is null
            ? new AccessTokenIssue(Roles: roles, AuthKind: AuthKinds.Registered)
            : issue with { Roles = roles, AuthKind = issue.AuthKind ?? AuthKinds.Registered };
        var refresh = tokens.CreateRefreshToken();
        await store.StoreRefreshTokenAsync(
            Guid.NewGuid(),
            user.Id,
            refresh.Hash,
            refresh.ExpiresAt,
            cancellationToken,
            issued.AuthKind,
            issued.Purpose,
            issued.RequestId);
        return new TokenResponse(
            tokens.CreateAccessToken(user, issued),
            refresh.Token,
            await ToResponseAsync(user, cancellationToken, roles, issued.AuthKind));
    }

    private static string DestinationOf(string? email, string? phone)
    {
        var normalizedEmail = email?.Trim() ?? "";
        if (normalizedEmail.Length > 0)
        {
            return normalizedEmail.ToLowerInvariant();
        }

        return ProfileRules.NormalizePhone(phone) ?? "";
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

    private async Task<AuthUserResponse> ToResponseAsync(
        IdentityUser user,
        CancellationToken cancellationToken,
        IReadOnlyList<string>? roles = null,
        string? authKind = null)
    {
        var (firstName, lastName) = ProfileRules.SplitName(user.FirstName, user.LastName, user.FullName);
        var resolved = roles is { Count: > 0 }
            ? RoleAuthorization.NormalizeRoles(roles)
            : RoleAuthorization.NormalizeRoles(await store.ListRolesAsync(user.Id, cancellationToken), user.Role);
        IReadOnlyList<PartnerMembershipDto> memberships = [];
        if (partners is not null)
        {
            memberships = (await partners.ListMembershipsForUserAsync(user.Id, cancellationToken))
                .Select(item => new PartnerMembershipDto(item.PartnerId, item.PartnerName, item.MembershipRole, item.Status))
                .ToArray();
        }

        return new(
            user.Id,
            user.Email,
            ProfileRules.DisplayName(firstName, lastName, user.FullName, user.Email),
            RoleAuthorization.PrimaryRole(resolved, user.Role),
            firstName,
            lastName,
            user.PhoneE164,
            user.Address,
            user.PhoneE164 is null ? null : ProfileRules.IndiaCountryCode,
            user.AccountStatus,
            resolved,
            memberships,
            authKind);
    }

    private static string? DenyPortal(string? portal, IdentityUser user, IReadOnlyList<string> storedRoles)
    {
        var wanted = portal?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(wanted) || wanted is "customer")
        {
            return null;
        }

        var roles = RoleAuthorization.NormalizeRoles(storedRoles, user.Role);
        if (wanted is "admin" && !RoleAuthorization.SatisfiesAdminWrite(roles))
        {
            return "This account is not an admin.";
        }

        if (wanted is "vendor" && !RoleAuthorization.SatisfiesPartnerWrite(roles))
        {
            return "This account is not a retreat partner.";
        }

        return null;
    }

    /// <summary>
    /// Vendor sessions are issued only after an active PartnerMembership is proven.
    /// A partner role without an approved, active membership is not enough.
    /// Admins may enter the vendor portal for operational support without a membership.
    /// </summary>
    private async Task<string?> DenyVendorMembershipAsync(
        string? portal,
        IReadOnlyList<string> storedRoles,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var wanted = portal?.Trim().ToLowerInvariant();
        if (wanted is not "vendor")
        {
            return null;
        }

        if (RoleAuthorization.SatisfiesAdminWrite(storedRoles))
        {
            return null;
        }

        if (partners is null)
        {
            return "This account is not linked to an approved partner.";
        }

        var memberships = await partners.ListMembershipsForUserAsync(userId, cancellationToken);
        if (!memberships.Any(item =>
                string.Equals(item.Status, "active", StringComparison.OrdinalIgnoreCase)))
        {
            return "This account is not linked to an approved partner.";
        }

        return null;
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
