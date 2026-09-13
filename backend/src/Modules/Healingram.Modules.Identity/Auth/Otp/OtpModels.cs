using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Healingram.Modules.Identity.Auth.Otp;

internal sealed class OtpSettings
{
    public const string DefaultLocalCode = "560142";

    public required string Provider { get; init; }
    public required string LocalCode { get; init; }
    public required bool DemoMode { get; init; }
    public int ExpiryMinutes { get; init; } = 10;
    public int MaxAttempts { get; init; } = 5;
    public int ResendSeconds { get; init; } = 30;
    public int MaxStartsPerHour { get; init; } = 30;

    public static OtpSettings From(IConfiguration configuration, bool development)
        => new()
        {
            Provider = configuration["Otp:Provider"] ?? "local",
            LocalCode = string.IsNullOrWhiteSpace(configuration["Otp:LocalCode"])
                ? DefaultLocalCode
                : configuration["Otp:LocalCode"]!,
            DemoMode = configuration.GetValue("DemoMode", development),
            ExpiryMinutes = configuration.GetValue("Otp:ExpiryMinutes", 10),
            MaxAttempts = configuration.GetValue("Otp:MaxAttempts", 5),
            ResendSeconds = configuration.GetValue("Otp:ResendSeconds", 30),
            MaxStartsPerHour = configuration.GetValue("Otp:MaxStartsPerHour", 30)
        };
}

internal sealed class OtpChallenge
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? PublicId { get; init; }
    public required string Destination { get; init; }
    public required string Channel { get; init; }
    public required string Purpose { get; init; }
    public required string CodeHash { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; init; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public required string Provider { get; init; }
    public string? ProviderReference { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
}

internal interface IOtpChallengeStore
{
    Task InsertAsync(OtpChallenge challenge, CancellationToken cancellationToken);
    Task<OtpChallenge?> FindLatestOpenAsync(string destination, string purpose, CancellationToken cancellationToken);
    Task UpdateAsync(OtpChallenge challenge, CancellationToken cancellationToken);
    Task SetProviderReferenceAsync(Guid id, string? providerReference, CancellationToken cancellationToken);
    Task<bool> TryIncrementAttemptsAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> TryConsumeAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken);
    Task<int> CountCreatedSinceAsync(string destination, DateTimeOffset since, CancellationToken cancellationToken);
}

internal static class OtpHashes
{
    public static string Hash(string code, string purpose, string destination)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{purpose}\n{destination}\n{code.Trim()}")))
            .ToLowerInvariant();
}
