using Healingram.Contracts.Availability;
using Healingram.Modules.Availability.Persistence;

namespace Healingram.Modules.Availability.Application;

internal sealed class RequestAccessLookup(IAvailabilityStore store) : IRequestAccessLookup
{
    public async Task<RequestAccessMatch?> FindGuestMatchAsync(
        string publicId,
        string? email,
        string? phone,
        CancellationToken cancellationToken)
    {
        var entity = await store.FindByPublicIdAsync(publicId.Trim(), cancellationToken);
        if (entity is null || entity.CustomerUserId is null)
        {
            return null;
        }

        var normalizedEmail = email?.Trim() ?? "";
        if (normalizedEmail.Length > 0)
        {
            return entity.CustomerEmail.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)
                ? new RequestAccessMatch(entity.PublicId, entity.CustomerUserId.Value)
                : null;
        }

        var digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
        var stored = new string(entity.CustomerPhone.Where(char.IsDigit).ToArray());
        if (digits.Length == 0 || stored.Length == 0)
        {
            return null;
        }

        return stored.EndsWith(digits, StringComparison.Ordinal) || digits.EndsWith(stored, StringComparison.Ordinal)
            ? new RequestAccessMatch(entity.PublicId, entity.CustomerUserId.Value)
            : null;
    }
}
