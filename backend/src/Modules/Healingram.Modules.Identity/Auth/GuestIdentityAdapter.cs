using Healingram.Contracts.Identity;
using Healingram.Modules.Identity.Data;

namespace Healingram.Modules.Identity.Auth;

internal sealed class GuestIdentityAdapter(IIdentityStore store) : IGuestIdentityPort
{
    public async Task<Guid> EnsureCustomerAsync(
        string email,
        string phoneE164,
        string displayName,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();
        var normalizedPhone = ProfileRules.NormalizePhone(phoneE164) ?? phoneE164.Trim();
        var existing = await store.FindByEmailAsync(normalizedEmail, cancellationToken)
                       ?? await store.FindByPhoneAsync(normalizedPhone, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var (firstName, lastName) = ProfileRules.SplitName(null, null, displayName);
        var user = new IdentityUser(
            Guid.NewGuid(),
            normalizedEmail,
            displayName.Trim(),
            Roles.Customer,
            "active",
            firstName,
            lastName,
            normalizedPhone,
            null,
            AccountStatuses.Guest);

        try
        {
            user = await store.CreateGuestAsync(user, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            var raced = await store.FindByEmailAsync(normalizedEmail, cancellationToken);
            if (raced is not null)
            {
                return raced.Id;
            }

            throw;
        }

        return user.Id;
    }
}
