using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class DuplicateEmailTests
{
    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        var service = CreateService();

        var first = await service.RegisterAsync(
            new RegisterRequest("guest@local.test", "Local123!", "First", "admin"),
            CancellationToken.None);
        var second = await service.RegisterAsync(
            new RegisterRequest("Guest@local.test", "Local123!", "Second", "partner"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Created, first.Status);
        Assert.Equal(Roles.Customer, first.Tokens?.User.Role);
        Assert.Equal(AuthStatus.DuplicateEmail, second.Status);
        Assert.Equal("Email already registered", second.Error);
    }

    private static AuthService CreateService()
        => new(
            new InMemoryIdentityStore(),
            new AspNetIdentityPasswordHasher(),
            new StubTokenService(),
            NullLogger<AuthService>.Instance);
}
