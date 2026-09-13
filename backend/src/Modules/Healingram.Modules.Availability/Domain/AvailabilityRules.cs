using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Healingram.Contracts.Availability;
using Healingram.Contracts.Catalog;

namespace Healingram.Modules.Availability.Domain;

internal static class AvailabilityActions
{
    public const string Confirm = "confirm";
    public const string Alternative = "alternative";
    public const string Unavailable = "unavailable";
    public const string AcceptAlternative = "accept-alternative";
}

internal static class StatusMachine
{
    public static bool IsKnown(string? status)
        => status is AvailabilityStatuses.Requested
            or AvailabilityStatuses.Confirmed
            or AvailabilityStatuses.AlternativeOffered
            or AvailabilityStatuses.Unavailable;

    public static bool CanTransition(string from, string to, string action)
    {
        if (string.Equals(to, "PAID", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return (from, to, action) switch
        {
            (AvailabilityStatuses.Requested, AvailabilityStatuses.Confirmed, AvailabilityActions.Confirm) => true,
            (AvailabilityStatuses.Requested, AvailabilityStatuses.AlternativeOffered, AvailabilityActions.Alternative) => true,
            (AvailabilityStatuses.Requested, AvailabilityStatuses.Unavailable, AvailabilityActions.Unavailable) => true,
            (AvailabilityStatuses.AlternativeOffered, AvailabilityStatuses.Confirmed, AvailabilityActions.AcceptAlternative) => true,
            (AvailabilityStatuses.AlternativeOffered, AvailabilityStatuses.Unavailable, AvailabilityActions.Unavailable) => true,
            _ => false
        };
    }

    public static string? RejectReason(string from, string to, string action)
    {
        if (string.Equals(to, "PAID", StringComparison.OrdinalIgnoreCase))
        {
            return "Admin and partners cannot mark PAID";
        }

        if (CanTransition(from, to, action))
        {
            return null;
        }

        return $"Cannot {action} from {from} to {to}";
    }
}

internal sealed record ValidatedStay(
    string IdempotencyKey,
    string RetreatSlug,
    string ProgrammeSlug,
    int DurationNights,
    string Occupancy,
    int Guests,
    string CheckIn,
    string CustomerName,
    string Email,
    string Phone);

internal static class StayRules
{
    public static IReadOnlyList<string> Validate(
        string? idempotencyKey,
        string? retreatSlug,
        string? programmeSlug,
        int? durationNights,
        string? occupancy,
        int? guests,
        string? checkIn,
        string? customerName,
        string? email,
        string? phone,
        out ValidatedStay? stay)
    {
        var details = new List<string>();

        var key = idempotencyKey?.Trim() ?? "";
        if (key.Length is < 8 or > 128)
        {
            details.Add("idempotencyKey is required (8–128 characters)");
        }

        var retreat = retreatSlug?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(retreat))
        {
            details.Add("retreatSlug is required");
        }

        var programme = programmeSlug?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(programme))
        {
            details.Add("programmeSlug is required");
        }

        if (durationNights is null or < 1 or > 90)
        {
            details.Add("durationNights must be between 1 and 90");
        }

        var occ = occupancy?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(occ) || occ.Length > 40)
        {
            details.Add("occupancy is required");
        }

        if (guests is null or < 1 or > 30)
        {
            details.Add("guests must be between 1 and 30");
        }

        var date = checkIn?.Trim() ?? "";
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            details.Add("checkIn must be an ISO date (yyyy-MM-dd)");
        }

        var name = customerName?.Trim() ?? "";
        if (name.Length < 2)
        {
            details.Add("customerName is required");
        }

        var mail = email?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(mail) || !mail.Contains('@'))
        {
            details.Add("email is required");
        }

        var rawPhone = phone?.Trim() ?? "";
        var digits = new string(rawPhone.Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
        {
            details.Add("phone is required");
        }

        if (details.Count > 0)
        {
            stay = null;
            return details;
        }

        stay = new ValidatedStay(
            key,
            retreat,
            programme,
            durationNights!.Value,
            occ,
            guests!.Value,
            date,
            name,
            mail,
            rawPhone);
        return details;
    }

    public static string Fingerprint(ValidatedStay stay)
    {
        var canonical = string.Join('|',
            stay.RetreatSlug.ToLowerInvariant(),
            stay.ProgrammeSlug.ToLowerInvariant(),
            stay.DurationNights.ToString(CultureInfo.InvariantCulture),
            stay.Occupancy.ToLowerInvariant(),
            stay.Guests.ToString(CultureInfo.InvariantCulture),
            stay.CheckIn,
            stay.CustomerName.ToLowerInvariant(),
            stay.Email.ToLowerInvariant(),
            new string(stay.Phone.Where(char.IsDigit).ToArray()));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexString(bytes);
    }

    public static string FingerprintFromStored(
        string snapshotJson,
        string retreatSlug,
        string programmeSlug,
        string customerName,
        string email,
        string phone,
        string idempotencyKey)
    {
        var snapshot = JsonNode.Parse(snapshotJson) as JsonObject;
        var duration = snapshot?["durationNights"]?.GetValue<int>() ?? 0;
        var occupancy = snapshot?["occupancy"]?.GetValue<string>() ?? "";
        var guests = snapshot?["guests"]?.GetValue<int>() ?? 0;
        var checkIn = snapshot?["checkIn"]?.GetValue<string>() ?? "";
        var retreat = string.IsNullOrWhiteSpace(retreatSlug)
            ? snapshot?["retreatSlug"]?.GetValue<string>() ?? ""
            : retreatSlug;
        var programme = string.IsNullOrWhiteSpace(programmeSlug)
            ? snapshot?["programmeSlug"]?.GetValue<string>() ?? ""
            : programmeSlug;

        return Fingerprint(new ValidatedStay(
            idempotencyKey,
            retreat,
            programme,
            duration,
            occupancy,
            guests,
            checkIn,
            customerName,
            email,
            phone));
    }
}

internal static class PriceSnapshotFactory
{
    public static string Capture(ValidatedStay stay, DateTimeOffset capturedAt)
    {
        var node = new JsonObject
        {
            ["retreatSlug"] = stay.RetreatSlug,
            ["programmeSlug"] = stay.ProgrammeSlug,
            ["checkIn"] = stay.CheckIn,
            ["durationNights"] = stay.DurationNights,
            ["occupancy"] = stay.Occupancy,
            ["guests"] = stay.Guests,
            ["priceStatus"] = "ON_REQUEST",
            ["baseAmount"] = null,
            ["taxAmount"] = null,
            ["taxDisplay"] = "not_confirmed",
            ["totalAmount"] = null,
            ["currency"] = "INR",
            ["label"] = "Price on request",
            ["capturedAt"] = capturedAt.ToUniversalTime().ToString("O"),
            ["roomType"] = stay.Occupancy,
            ["pricingVersion"] = "1"
        };

        return node.ToJsonString(AvailabilityJson.Options);
    }

    public static string FromQuote(CatalogQuote quote, ValidatedStay stay, DateTimeOffset capturedAt)
    {
        var node = JsonNode.Parse(quote.SnapshotJson) as JsonObject ?? [];
        node["retreatSlug"] = stay.RetreatSlug;
        node["programmeSlug"] = stay.ProgrammeSlug;
        node["checkIn"] = stay.CheckIn;
        node["durationNights"] = stay.DurationNights;
        node["occupancy"] = stay.Occupancy;
        node["guests"] = stay.Guests;
        node["quoteId"] = quote.Id.ToString();
        node["pricingVersion"] = quote.PricingVersion;
        node["currency"] = quote.Currency;
        node["priceStatus"] = quote.PriceStatus;
        node["baseAmount"] = quote.BaseAmount is { } baseAmount ? JsonValue.Create(baseAmount) : null;
        node["taxAmount"] = quote.TaxAmount is { } tax ? JsonValue.Create(tax) : null;
        node["totalAmount"] = quote.TotalAmount is { } total ? JsonValue.Create(total) : null;
        node["capturedAt"] = capturedAt.ToUniversalTime().ToString("O");
        node["roomType"] = stay.Occupancy;
        return node.ToJsonString(AvailabilityJson.Options);
    }
}

internal static class PublicIds
{
    public static string Request(int year, long sequence) => $"HR-{year}-{sequence:D5}";

    public static bool LooksHumanReadable(string publicId)
        => publicId.StartsWith("HR-", StringComparison.Ordinal)
           && publicId.Length >= 10
           && publicId.Count(ch => ch == '-') == 2;
}

internal static class AvailabilityJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
