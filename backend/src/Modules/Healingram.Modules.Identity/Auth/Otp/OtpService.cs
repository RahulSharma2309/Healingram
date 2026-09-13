using Healingram.Contracts.Otp;
using Microsoft.Extensions.Logging;

namespace Healingram.Modules.Identity.Auth.Otp;

internal sealed class OtpService(
    IOtpProvider provider,
    IOtpChallengeStore store,
    OtpSettings settings,
    TimeProvider clock,
    ILogger<OtpService> logger) : IOtpService
{
    public async Task<OtpStartResult> StartAsync(OtpStartCommand command, CancellationToken cancellationToken)
    {
        if (!OtpPurposes.IsKnown(command.Purpose))
        {
            return new OtpStartResult(false, null, "purpose is not valid");
        }

        var destination = command.Destination.Trim().ToLowerInvariant();
        if (destination.Length == 0)
        {
            return new OtpStartResult(false, null, "destination is required");
        }

        var now = clock.GetUtcNow();
        var latest = await store.FindLatestOpenAsync(destination, command.Purpose, cancellationToken);
        if (latest is not null && latest.CreatedAt.AddSeconds(settings.ResendSeconds) > now)
        {
            return new OtpStartResult(false, null, "wait before requesting another code");
        }

        var plain = string.Equals(provider.Name, OtpProviders.Local, StringComparison.OrdinalIgnoreCase)
            ? settings.LocalCode
            : Random.Shared.Next(100000, 999999).ToString();
        var challenge = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            UserId = command.UserId,
            PublicId = command.PublicId,
            Destination = destination,
            Channel = command.Channel,
            Purpose = command.Purpose,
            CodeHash = OtpHashes.Hash(plain, command.Purpose, destination),
            ExpiresAt = now.AddMinutes(settings.ExpiryMinutes),
            MaxAttempts = settings.MaxAttempts,
            Provider = provider.Name,
            CreatedAt = now
        };

        await store.InsertAsync(challenge, cancellationToken);
        var dispatched = await provider.DispatchAsync(
            new OtpDispatchRequest(challenge.Id, destination, command.Channel, command.Purpose, plain),
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(dispatched.ProviderReference))
        {
            challenge.ProviderReference = dispatched.ProviderReference;
            await store.SetProviderReferenceAsync(challenge.Id, dispatched.ProviderReference, cancellationToken);
        }

        logger.LogInformation("OTP challenge started for purpose {Purpose}", command.Purpose);
        return new OtpStartResult(dispatched.Sent, dispatched.DevelopmentCode, null);
    }

    public async Task<OtpVerifyResult> VerifyAsync(OtpVerifyCommand command, CancellationToken cancellationToken)
    {
        if (!OtpPurposes.IsKnown(command.Purpose))
        {
            return new OtpVerifyResult(false, "purpose is not valid", null, null);
        }

        var destination = command.Destination.Trim().ToLowerInvariant();
        var challenge = await store.FindLatestOpenAsync(destination, command.Purpose, cancellationToken);
        if (challenge is null)
        {
            return new OtpVerifyResult(false, "verification code is not right", null, null);
        }

        var now = clock.GetUtcNow();
        if (challenge.ConsumedAt is not null)
        {
            return new OtpVerifyResult(false, "verification code was already used", null, null);
        }

        if (challenge.ExpiresAt <= now)
        {
            return new OtpVerifyResult(false, "verification code has expired", null, null);
        }

        if (challenge.Attempts >= challenge.MaxAttempts)
        {
            return new OtpVerifyResult(false, "too many attempts", null, null);
        }

        if (!ScopedIdsMatch(challenge.PublicId, command.PublicId))
        {
            await store.TryIncrementAttemptsAsync(challenge.Id, cancellationToken);
            return new OtpVerifyResult(false, "verification code is not right", null, null);
        }

        var expected = OtpHashes.Hash(command.Code, command.Purpose, destination);
        if (!string.Equals(expected, challenge.CodeHash, StringComparison.Ordinal))
        {
            await store.TryIncrementAttemptsAsync(challenge.Id, cancellationToken);
            return new OtpVerifyResult(false, "verification code is not right", null, null);
        }

        if (!await store.TryConsumeAsync(challenge.Id, now, cancellationToken))
        {
            return new OtpVerifyResult(false, "verification code was already used", null, null);
        }

        logger.LogInformation("OTP challenge consumed for purpose {Purpose}", command.Purpose);
        return new OtpVerifyResult(true, null, challenge.UserId, challenge.PublicId ?? command.PublicId);
    }

    internal static bool ScopedIdsMatch(string? challengePublicId, string? commandPublicId)
    {
        var left = string.IsNullOrWhiteSpace(challengePublicId) ? null : challengePublicId.Trim();
        var right = string.IsNullOrWhiteSpace(commandPublicId) ? null : commandPublicId.Trim();
        if (left is null && right is null)
        {
            return true;
        }

        return left is not null
               && right is not null
               && left.Equals(right, StringComparison.OrdinalIgnoreCase);
    }
}
