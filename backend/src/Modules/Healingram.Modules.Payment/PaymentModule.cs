using Healingram.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Payment;

public sealed class PaymentModule : IAppModule
{
    public string Name => "Payment";
    public string Schema => "payment";
    public void Register(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/api/payment/ready", () => Results.Ok(new { module = Name })).WithTags("Payment");
}
