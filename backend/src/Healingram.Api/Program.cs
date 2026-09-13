using Healingram.BuildingBlocks.Health;
using Healingram.BuildingBlocks.Modules;
using Healingram.BuildingBlocks.Notifications;
using Healingram.BuildingBlocks.Observability;
using Healingram.BuildingBlocks.Persistence;
using Healingram.Modules.Availability;
using Healingram.Modules.Booking;
using Healingram.Modules.Catalog;
using Healingram.Modules.Identity;
using Healingram.Modules.Leads;
using Healingram.Modules.Matching;
using Healingram.Modules.Partners;
using Healingram.Modules.Payment;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, _, config) =>
{
    config
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("service", "healingram-api")
        .WriteTo.Console();

    var seqUrl = ctx.Configuration["Seq:Url"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        config.WriteTo.Seq(seqUrl);
    }
});

builder.Services.AddOpenApi();
builder.Services.AddScoped<SchemaInstaller>();

IAppModule[] modules =
[
    new IdentityModule(),
    new CatalogModule(),
    new MatchingModule(),
    new AvailabilityModule(),
    new BookingModule(),
    new PaymentModule(),
    new LeadsModule(),
    new PartnersModule()
];

foreach (var module in modules)
{
    module.Register(builder.Services, builder.Configuration);
    builder.Services.AddSingleton<IAppModule>(module);
}

builder.Services.AddHealingramOutbox(builder.Configuration);

var otlp = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("healingram-api"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation();
        t.AddHttpClientInstrumentation();
        if (!string.IsNullOrWhiteSpace(otlp))
        {
            t.AddOtlpExporter(o => o.Endpoint = new Uri(otlp));
        }
    });

var app = builder.Build();

app.UseHealingramCorrelationId();
app.UseAuthentication();
app.UseAuthorization();
app.UseSerilogRequestLogging();

if (app.Configuration.GetValue("Schema:ApplyOnStartup", true))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<SchemaInstaller>().EnsureCreatedAsync(app.Lifetime.ApplicationStopping);
}

app.MapOpenApi();
app.MapGet("/api/health", () => Results.Ok(new HealthResponse("ok", "0.1.0", DateTimeOffset.UtcNow)));
app.MapGet("/api/meta", () => Results.Ok(new MetaResponse(
    app.Environment.EnvironmentName,
    "healingram-api",
    "0.1.0")));

foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();

public partial class Program;
