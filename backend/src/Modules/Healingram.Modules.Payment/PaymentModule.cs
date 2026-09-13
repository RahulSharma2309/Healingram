using System.Runtime.CompilerServices;
using Healingram.BuildingBlocks.Modules;
using Healingram.Contracts.Payment;
using Healingram.Modules.Payment.Application;
using Healingram.Modules.Payment.Infrastructure;
using Healingram.Modules.Payment.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Healingram.Modules.Payment.Tests")]

namespace Healingram.Modules.Payment;

public sealed class PaymentModule : IAppModule
{
    public string Name => "Payment";
    public string Schema => "payment";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(PaymentSettings.From(configuration));
        services.AddSingleton<IPaymentProvider, LocalPaymentProvider>();
        services.AddScoped<IPaymentStore, PostgresPaymentStore>();
        services.AddScoped<PaymentService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/payment/ready", () => Results.Ok(new { module = Name })).WithTags("Payment");
        PaymentEndpoints.Map(app);
    }
}
