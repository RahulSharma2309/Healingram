using Healingram.Contracts.Payment;
using Healingram.Modules.Payment.Application;

namespace Healingram.Modules.Payment.Infrastructure;

internal static class PaymentProviderFactory
{
    public static IPaymentProvider Create(PaymentSettings settings)
    {
        var configured = PaymentProviders.Normalize(settings.ProviderName);
        return configured switch
        {
            PaymentProviders.Local or PaymentProviders.Fake => new LocalPaymentProvider(settings),
            PaymentProviders.Razorpay => throw new InvalidOperationException(
                "Payment:Provider=razorpay is declared but RazorpayPaymentProvider is not in this build."),
            PaymentProviders.Stripe => throw new InvalidOperationException(
                "Payment:Provider=stripe is declared but StripePaymentProvider is not in this build."),
            _ => throw new InvalidOperationException(
                $"Unknown Payment:Provider '{settings.ProviderName}'. Refusing to start.")
        };
    }
}
