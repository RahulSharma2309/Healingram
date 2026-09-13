using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Healingram.Api.Tests;

public class PortalAuthorizationTests : IClassFixture<HealingramApiFactory>
{
    private readonly HealingramApiFactory _factory;

    public PortalAuthorizationTests(HealingramApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/admin/availability")]
    [InlineData("/api/partner/availability")]
    [InlineData("/api/users/me")]
    public async Task Anonymous_staff_and_account_routes_are_401(string path)
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Customer_token_cannot_call_partner_or_admin_routes()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(Roles.Customer));

        var partner = await client.GetAsync("/api/partner/availability");
        var admin = await client.GetAsync("/api/admin/availability");
        var confirm = await client.PostAsync("/api/availability/requests/HR-2026-10001/confirm", null);

        Assert.Equal(HttpStatusCode.Forbidden, partner.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, confirm.StatusCode);
    }

    private static string Token(string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("healingram-local-dev-jwt-key-change-me-32"));
        var jwt = new JwtSecurityToken(
            issuer: "healingram",
            audience: "healingram",
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim("sub", Guid.NewGuid().ToString()),
                new Claim("role", role),
                new Claim(ClaimTypes.Role, role),
                new Claim("account_status", "registered")
            ],
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static class Roles
    {
        public const string Customer = "customer";
    }
}
