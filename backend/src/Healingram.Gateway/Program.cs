using Healingram.BuildingBlocks.Observability;
using Healingram.Gateway;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, _, config) =>
{
    config.Enrich.FromLogContext().Enrich.WithProperty("service", "healingram-gateway").WriteTo.Console();
    var seqUrl = ctx.Configuration["Seq:Url"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        config.WriteTo.Seq(seqUrl);
    }
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddHealingramCorrelationTransforms();

var otlp = builder.Configuration["OpenTelemetry:OtlpEndpoint"];
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("healingram-gateway"))
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
app.UseCors();
app.UseHealingramCorrelationId();
app.UseHealingramCorrelationForward();
app.MapGet("/api/gateway/health", () => Results.Ok(new { status = "ok", service = "gateway" }));
app.MapReverseProxy();
app.Run();

public partial class Program;
