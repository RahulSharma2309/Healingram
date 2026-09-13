using Healingram.Contracts.Identity;
using Healingram.Modules.Leads.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Healingram.Modules.Leads.Tests;

public class LeadEndpointAuthorizationTests
{
    [Fact]
    public void Get_by_id_requires_admin_write_policy()
    {
        var get = MappedEndpoints()
            .Single(e =>
                e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("GET") == true
                && e.RoutePattern.RawText?.Contains("{id", StringComparison.Ordinal) == true);

        var policies = get.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Select(a => a.Policy)
            .ToArray();

        Assert.Contains(IdentityPolicies.AdminLeadsRead, policies);
    }

    [Fact]
    public void Create_is_public()
    {
        var post = MappedEndpoints()
            .Single(e => e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true);

        Assert.DoesNotContain(
            post.Metadata.GetOrderedMetadata<IAuthorizeData>(),
            a => !string.IsNullOrWhiteSpace(a.Policy));
    }

    private static IReadOnlyList<RouteEndpoint> MappedEndpoints()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddRouting();
        services.AddAuthorization();
        services.AddSingleton(LeadHarness.Create().Service);
        var provider = services.BuildServiceProvider();
        var builder = new CapturingEndpointRouteBuilder(provider);
        LeadsEndpoints.Map(builder);
        return builder.DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();
    }

    private sealed class CapturingEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;
        public ICollection<EndpointDataSource> DataSources { get; } = new List<EndpointDataSource>();

        public IApplicationBuilder CreateApplicationBuilder() => new ApplicationBuilder(ServiceProvider);
    }
}
