using Healingram.BuildingBlocks.Observability;
using Yarp.ReverseProxy.Transforms;

namespace Healingram.Gateway;

internal static class CorrelationProxyExtensions
{
    public static IReverseProxyBuilder AddHealingramCorrelationTransforms(this IReverseProxyBuilder builder)
    {
        builder.AddTransforms(context =>
        {
            context.AddRequestTransform(static transformContext =>
            {
                var id = GetCorrelationId(transformContext.HttpContext);
                transformContext.ProxyRequest.Headers.Remove(CorrelationId.HeaderName);
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation(CorrelationId.HeaderName, id);
                return ValueTask.CompletedTask;
            });

            context.AddResponseTransform(static transformContext =>
            {
                var id = GetCorrelationId(transformContext.HttpContext);
                transformContext.HttpContext.Response.Headers[CorrelationId.HeaderName] = id;
                return ValueTask.CompletedTask;
            });
        });

        return builder;
    }

    public static IApplicationBuilder UseHealingramCorrelationForward(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var id = GetCorrelationId(context);
            context.Request.Headers[CorrelationId.HeaderName] = id;
            context.Response.OnStarting(static state =>
            {
                var (http, correlationId) = ((HttpContext, string))state!;
                http.Response.Headers[CorrelationId.HeaderName] = correlationId;
                return Task.CompletedTask;
            }, (context, id));

            await next();
        });
    }

    internal static string GetCorrelationId(HttpContext http)
    {
        if (http.Items.TryGetValue(CorrelationId.ItemKey, out var value)
            && value is string id
            && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        return CorrelationId.Ensure(http.Request.Headers[CorrelationId.HeaderName].FirstOrDefault());
    }
}
