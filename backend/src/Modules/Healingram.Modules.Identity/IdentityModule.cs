using System.Runtime.CompilerServices;
using Healingram.BuildingBlocks.Modules;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Audit;
using Healingram.Contracts.Otp;
using Healingram.BuildingBlocks.Notifications;
using Healingram.Modules.Identity.Admin;
using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Auth.Otp;
using Healingram.Modules.Identity.Data;
using Healingram.Modules.Identity.Notifications;
using Healingram.Modules.Identity.Wishlist;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: InternalsVisibleTo("Healingram.Modules.Identity.Tests")]

namespace Healingram.Modules.Identity;

public sealed class IdentityModule : IAppModule
{
    public string Name => "Identity";
    public string Schema => "identity";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = JwtSettings.From(configuration);

        services.AddSingleton(jwt);
        services.AddSingleton(sp =>
            OtpSettings.From(configuration, sp.GetRequiredService<IHostEnvironment>().IsDevelopment()));
        services.AddSingleton(sp => OtpProviderFactory.Create(sp.GetRequiredService<OtpSettings>()));
        services.AddSingleton<IUserPasswordHasher, AspNetIdentityPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IIdentityStore, PostgresIdentityStore>();
        services.AddScoped<IOtpChallengeStore, PostgresOtpChallengeStore>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IGuestIdentityPort, GuestIdentityAdapter>();
        services.AddScoped<IAdminAuthorization, AdminAuthorization>();
        services.AddScoped<IAuditPort, PostgresAuditPort>();
        services.AddScoped<AuthService>();
        services.AddScoped<WishlistService>();
        services.AddScoped<IUserInboxPort, PostgresUserInbox>();
        services.AddHostedService<IdentitySeedHostedService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = jwt.CreateValidationParameters();
                options.MapInboundClaims = true;
            });

        IdentityAuthorization.AddPolicies(services);
    }

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/identity/ready", () => Results.Ok(new { module = Name })).WithTags("Identity");
        AuthEndpoints.Map(app);
        WishlistEndpoints.Map(app);
        NotificationEndpoints.Map(app);
        AdminOverviewEndpoints.Map(app);
    }
}
