using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Healingram.BuildingBlocks.Observability;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var id = CorrelationId.Ensure(context.Request.Headers[CorrelationId.HeaderName].FirstOrDefault());
        context.Items[CorrelationId.ItemKey] = id;
        context.Response.Headers[CorrelationId.HeaderName] = id;
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
        {
            await next(context);
        }
    }
}

public static class CorrelationIdExtensions
{
    public static IApplicationBuilder UseHealingramCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
