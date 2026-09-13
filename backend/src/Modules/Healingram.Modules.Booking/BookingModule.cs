using System.Runtime.CompilerServices;
using Healingram.BuildingBlocks.Modules;
using Healingram.Contracts.Booking;
using Healingram.Modules.Booking.Application;
using Healingram.Modules.Booking.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: InternalsVisibleTo("Healingram.Modules.Booking.Tests")]

namespace Healingram.Modules.Booking;

public sealed class BookingModule : IAppModule
{
    public string Name => "Booking";
    public string Schema => "booking";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IBookingStore, PostgresBookingStore>();
        services.AddScoped<IBookingCommands, BookingCommands>();
        services.AddScoped<IBookingPaymentPort, BookingPaymentPort>();
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
        => app.MapGet("/api/booking/ready", () => Results.Ok(new { module = Name })).WithTags("Booking");
}
