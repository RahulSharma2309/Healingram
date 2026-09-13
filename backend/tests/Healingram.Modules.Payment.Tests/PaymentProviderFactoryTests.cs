using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Infrastructure;
using Xunit;

namespace Healingram.Modules.Payment.Tests;

public class PaymentProviderFactoryTests
{
    [Fact]
    public void Local_and_fake_select_local_provider()
    {
        var local = PaymentProviderFactory.Create(new PaymentSettings { ProviderName = "local" });
        var fake = PaymentProviderFactory.Create(new PaymentSettings { ProviderName = "fake" });

        Assert.Equal("local", local.Name);
        Assert.Equal("local", fake.Name);
        Assert.DoesNotContain("razorpay", local.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Razorpay_and_unknown_fail_startup()
    {
        var razorpay = Assert.Throws<InvalidOperationException>(
            () => PaymentProviderFactory.Create(new PaymentSettings { ProviderName = "razorpay" }));
        var unknown = Assert.Throws<InvalidOperationException>(
            () => PaymentProviderFactory.Create(new PaymentSettings { ProviderName = "paypal" }));

        Assert.Contains("not in this build", razorpay.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unknown", unknown.Message, StringComparison.OrdinalIgnoreCase);
    }
}
