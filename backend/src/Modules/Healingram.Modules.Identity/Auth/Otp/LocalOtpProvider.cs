using Healingram.Contracts.Otp;

namespace Healingram.Modules.Identity.Auth.Otp;

internal sealed class LocalOtpProvider(OtpSettings settings) : IOtpProvider
{
    public string Name => "local";

    public Task<OtpDispatchResult> DispatchAsync(OtpDispatchRequest request, CancellationToken cancellationToken)
    {
        _ = request;
        _ = cancellationToken;
        return Task.FromResult(new OtpDispatchResult(
            true,
            "local-dev",
            settings.DemoMode ? settings.LocalCode : null));
    }
}
