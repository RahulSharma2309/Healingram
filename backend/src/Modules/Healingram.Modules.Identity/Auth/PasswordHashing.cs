using Microsoft.AspNetCore.Identity;

namespace Healingram.Modules.Identity.Auth;

internal interface IUserPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

internal sealed class AspNetIdentityPasswordHasher : IUserPasswordHasher
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password)
        => _hasher.HashPassword(new object(), password);

    public bool Verify(string passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(new object(), passwordHash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
