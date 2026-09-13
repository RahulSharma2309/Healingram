using Healingram.Contracts.Booking;
using Healingram.Contracts.Catalog;
using Healingram.Contracts.Identity;
using Healingram.Contracts.Partners;
using Healingram.Modules.Availability.Application;
using Microsoft.Extensions.Logging.Abstractions;

namespace Healingram.Modules.Availability.Tests.Fakes;

internal sealed class FakeCatalogReadPort(params string[] slugs) : ICatalogReadPort
{
    public Task<IReadOnlyList<string>> GetPublicRetreatSlugsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>(slugs);

    public Task<IReadOnlyList<PublishedRetreatMatchCard>> GetPublishedRetreatsForMatchAsync(
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<PublishedRetreatMatchCard>>([]);
}

internal sealed class RecordingBookingCommands : IBookingCommands
{
    public List<CreateAwaitingPaymentBooking> Calls { get; } = [];

    public Task<BookingRef> CreateAwaitingPaymentAsync(
        CreateAwaitingPaymentBooking command,
        CancellationToken cancellationToken)
    {
        Calls.Add(command);
        return Task.FromResult(new BookingRef(Guid.NewGuid(), "BK-2026-10001", BookingStatuses.AwaitingPayment));
    }
}

internal sealed class FakePartnerAccess : IPartnerAccess
{
    private readonly Dictionary<Guid, IReadOnlyList<string>> _byUser = [];

    public IReadOnlyList<string> DefaultSlugs { get; set; } = [];

    public void Map(Guid userId, params string[] slugs) => _byUser[userId] = slugs;

    public Task<IReadOnlyList<string>> ListRetreatSlugsForUserAsync(Guid userId, CancellationToken cancellationToken)
        => Task.FromResult(_byUser.TryGetValue(userId, out var slugs) ? slugs : DefaultSlugs);

    public async Task<bool> CanAccessRetreatAsync(Guid userId, string retreatSlug, CancellationToken cancellationToken)
    {
        var slugs = await ListRetreatSlugsForUserAsync(userId, cancellationToken);
        return slugs.Any(slug => slug.Equals(retreatSlug, StringComparison.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<PartnerMembership>> ListMembershipsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var slugs = _byUser.TryGetValue(userId, out var mapped) ? mapped : DefaultSlugs;
        if (slugs.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<PartnerMembership>>([]);
        }

        return Task.FromResult<IReadOnlyList<PartnerMembership>>(
        [
            new PartnerMembership(userId, "Test Partner", "manager", "active")
        ]);
    }
}

internal sealed class FakeGuestIdentityPort(Guid customerId) : IGuestIdentityPort
{
    public bool RequiresSignIn { get; set; }

    public Task<GuestIdentityResult> EnsureCustomerAsync(
        string email,
        string phoneE164,
        string displayName,
        CancellationToken cancellationToken)
        => Task.FromResult(RequiresSignIn
            ? new GuestIdentityResult(null, true)
            : new GuestIdentityResult(customerId, false));
}

internal sealed class FakeBookingPaymentPort : IBookingPaymentPort
{
    public HashSet<string> PaidPublicIds { get; } = new(StringComparer.Ordinal);

    public Task<BookingPaymentGate?> FindByPublicIdAsync(string publicId, CancellationToken cancellationToken)
    {
        if (!PaidPublicIds.Contains(publicId))
        {
            return Task.FromResult<BookingPaymentGate?>(null);
        }

        return Task.FromResult<BookingPaymentGate?>(
            new BookingPaymentGate(Guid.NewGuid(), "BK-PAID", BookingStatuses.Paid, null, publicId));
    }

    public Task<MarkPaidResult> MarkPaidAsync(Guid bookingId, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

internal sealed class AvailabilityFixture
{
    public required AvailabilityService Service { get; init; }
    public required InMemoryAvailabilityStore Store { get; init; }
    public required RecordingBookingCommands Bookings { get; init; }
    public required FakePartnerAccess Partners { get; init; }
    public required FakeBookingPaymentPort Payments { get; init; }
    public required FakeGuestIdentityPort Guests { get; init; }

    public void Deconstruct(
        out AvailabilityService service,
        out InMemoryAvailabilityStore store,
        out RecordingBookingCommands bookings)
    {
        service = Service;
        store = Store;
        bookings = Bookings;
    }
}

internal static class AvailabilityHarness
{
    public const string PublishedSlug = "published-retreat";

    public static AvailabilityFixture Create(params string[] catalogSlugs)
        => Create(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), catalogSlugs);

    public static AvailabilityFixture Create(Guid guestCustomerId, params string[] catalogSlugs)
    {
        var slugs = catalogSlugs.Length == 0 ? new[] { PublishedSlug } : catalogSlugs;
        var store = new InMemoryAvailabilityStore();
        var bookings = new RecordingBookingCommands();
        var partners = new FakePartnerAccess();
        partners.Map(Partner.UserId!.Value, slugs);
        var payments = new FakeBookingPaymentPort();
        var guests = new FakeGuestIdentityPort(guestCustomerId);
        var service = new AvailabilityService(
            store,
            new FakeCatalogReadPort(slugs),
            bookings,
            payments,
            partners,
            guests,
            TimeProvider.System,
            NullLogger<AvailabilityService>.Instance);
        return new AvailabilityFixture
        {
            Service = service,
            Store = store,
            Bookings = bookings,
            Partners = partners,
            Payments = payments,
            Guests = guests
        };
    }

    public static CreateAvailabilityRequest Request(
        string key = "idem-key-001",
        string retreat = PublishedSlug,
        string programme = "panchakarma",
        int nights = 7,
        string occupancy = "double",
        int guests = 2,
        string checkIn = "2026-11-02",
        string name = "Guest Local",
        string email = "guest@local.test",
        string phone = "+919876543210")
        => new(key, retreat, programme, nights, occupancy, guests, checkIn, name, email, phone);

    public static Actor Customer { get; } = new(Roles.Customer, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    public static Actor Partner { get; } = new(Roles.Partner, Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    public static Actor Admin { get; } = new(Roles.Admin, Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
}
