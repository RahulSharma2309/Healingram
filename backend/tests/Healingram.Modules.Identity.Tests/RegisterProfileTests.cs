using Healingram.Modules.Identity.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class RegisterProfileTests
{
    [Fact]
    public async Task Register_stores_split_name_phone_and_optional_address()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(
            new RegisterRequest(
                "rahul@local.test",
                "Local123!",
                null,
                null,
                "Rahul",
                "Sharma",
                "9876543210",
                "Local123!",
                "Whitefield"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Created, result.Status);
        Assert.Equal("Rahul Sharma", result.Tokens?.User.FullName);
        Assert.Equal("Rahul", result.Tokens?.User.FirstName);
        Assert.Equal("Sharma", result.Tokens?.User.LastName);
        Assert.Equal("+919876543210", result.Tokens?.User.Phone);
        Assert.Equal("+91", result.Tokens?.User.PhoneCountryCode);
        Assert.Equal("Whitefield", result.Tokens?.User.Address);
    }

    [Fact]
    public async Task Register_rejects_password_mismatch()
    {
        var result = await CreateService().RegisterAsync(
            new RegisterRequest(
                "rahul@local.test",
                "Local123!",
                null,
                null,
                "Rahul",
                "Sharma",
                "9876543210",
                "Local123?",
                null),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Validation, result.Status);
        Assert.Contains("passwords do not match", result.Details ?? []);
    }

    [Fact]
    public async Task Update_profile_changes_details()
    {
        var service = CreateService();
        var created = await service.RegisterAsync(ValidRegister("rahul@local.test"), CancellationToken.None);
        var userId = created.Tokens!.User.Id;

        var updated = await service.UpdateProfileAsync(
            userId,
            new UpdateProfileRequest("Priya", "Nair", "9988776655", "priya@local.test", "Alappuzha"),
            CancellationToken.None);

        Assert.Equal(AuthStatus.Ok, updated.Status);
        Assert.Equal("Priya Nair", updated.User?.FullName);
        Assert.Equal("+919988776655", updated.User?.Phone);
        Assert.Equal("Alappuzha", updated.User?.Address);
        Assert.Equal("priya@local.test", updated.User?.Email);
    }

    [Fact]
    public async Task Update_profile_rejects_another_users_email()
    {
        var service = CreateService();
        await service.RegisterAsync(ValidRegister("one@local.test"), CancellationToken.None);
        var second = await service.RegisterAsync(
            ValidRegister("two@local.test") with { FirstName = "Two" },
            CancellationToken.None);

        var updated = await service.UpdateProfileAsync(
            second.Tokens!.User.Id,
            new UpdateProfileRequest("Two", "Person", "9876543210", "one@local.test", null),
            CancellationToken.None);

        Assert.Equal(AuthStatus.DuplicateEmail, updated.Status);
    }

    private static RegisterRequest ValidRegister(string email)
        => new(email, "Local123!", null, null, "Rahul", "Sharma", "9876543210", "Local123!", null);

    private static AuthService CreateService()
        => new(
            new InMemoryIdentityStore(),
            new AspNetIdentityPasswordHasher(),
            new StubTokenService(),
            NullLogger<AuthService>.Instance);
}
