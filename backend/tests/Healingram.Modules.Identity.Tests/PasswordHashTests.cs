using Healingram.Modules.Identity.Auth;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class PasswordHashTests
{
    [Fact]
    public void Hash_then_verify_succeeds_and_never_stores_plaintext()
    {
        var hasher = new AspNetIdentityPasswordHasher();
        const string password = "Local123!";

        var hash = hasher.Hash(password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.DoesNotContain(password, hash, StringComparison.Ordinal);
        Assert.True(hasher.Verify(hash, password));
        Assert.False(hasher.Verify(hash, "wrong-password"));
    }
}
