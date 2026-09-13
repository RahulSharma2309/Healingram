using Healingram.Modules.Identity.Auth;
using Healingram.Modules.Identity.Data;

namespace Healingram.Modules.Identity.Tests;

internal sealed class StubTokenService : ITokenService
{
    private int _issued;

    public string CreateAccessToken(IdentityUser user) => $"access-{user.Id}";

    public IssuedRefreshToken CreateRefreshToken()
    {
        _issued++;
        var token = $"refresh-{_issued}";
        return new IssuedRefreshToken(token, HashRefreshToken(token), DateTimeOffset.UtcNow.AddDays(7));
    }

    public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
}
