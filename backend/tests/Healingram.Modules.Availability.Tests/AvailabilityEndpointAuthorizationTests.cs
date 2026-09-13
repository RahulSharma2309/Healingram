using Healingram.Contracts.Identity;
using Healingram.Modules.Availability.Tests.Fakes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Healingram.Modules.Availability.Tests;

public class AvailabilityEndpointAuthorizationTests
{
    [Fact]
    public void Admin_list_requires_requests_read()
    {
        var get = MappedEndpoints()
            .Single(e => e.RoutePattern.RawText == "/api/admin/availability");
        var policies = get.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy).ToArray();
        Assert.Contains(IdentityPolicies.AdminRequestsRead, policies);
    }

    [Fact]
    public void Admin_note_requires_requests_manage()
    {
        var post = MappedEndpoints()
            .Single(e => e.RoutePattern.RawText == "/api/admin/availability/{publicId}/note");
        var policies = post.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy).ToArray();
        Assert.Contains(IdentityPolicies.AdminRequestsManage, policies);
    }

    private static IReadOnlyList<RouteEndpoint> MappedEndpoints()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddRouting();
        services.AddAuthorization();
        services.AddSingleton(AvailabilityHarness.Create().Service);
        var provider = services.BuildServiceProvider();
        var builder = new CapturingEndpointRouteBuilder(provider);
        AvailabilityEndpoints.Map(builder);
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
