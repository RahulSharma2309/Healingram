using Healingram.BuildingBlocks.Modules;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Healingram.Modules.Booking;

public sealed class BookingModule : IAppModule
{
    public string Name => "Booking";
    public string Schema => "booking";
    public void Register(IServiceCollection services, IConfiguration configuration) { }
    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/api/booking/ready", () => Results.Ok(new { module = Name })).WithTags("Booking");
}
