using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Healingram.Api.RateLimiting;

internal static class RateLimitSetup
{
    public static readonly string[] PolicyNames =
    [
        "sensitive",
        "auth-login",
        "auth-register",
        "otp-send",
        "otp-verify",
        "payment-create",
        "payment-simulate"
    ];

    public static IServiceCollection AddHealingramRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            foreach (var name in PolicyNames)
            {
                var policy = name;
                var permit = configuration.GetValue($"RateLimiting:Policies:{policy}:PermitLimit", DefaultPermit(policy));
                var windowSeconds = configuration.GetValue($"RateLimiting:Policies:{policy}:WindowSeconds", 60);
                options.AddPolicy(policy, httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = Math.Max(1, permit),
                            Window = TimeSpan.FromSeconds(Math.Max(1, windowSeconds)),
                            QueueLimit = 0
                        }));
            }
        });

        return services;
    }

    private static int DefaultPermit(string policy)
        => policy switch
        {
            "auth-login" => 20,
            "auth-register" => 10,
            "otp-send" => 10,
            "otp-verify" => 20,
            "payment-create" => 20,
            "payment-simulate" => 10,
            _ => 40
        };
}
