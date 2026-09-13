using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Nodes;
using Healingram.Modules.Leads.Application;

namespace Healingram.Modules.Leads.Domain;

internal static class LeadStatuses
{
    public const string New = "NEW";
}

internal sealed record ValidatedLead(
    string FullName,
    string PhoneE164,
    string Email,
    bool WhatsappConsent,
    string? Source,
    string ContextJson);

internal static class LeadJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

internal static class LeadRules
{
    private static readonly HashSet<string> ReservedContextKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "fullName", "phone", "email", "whatsappConsent", "source"
    };

    public static IReadOnlyList<string> Validate(CreateLeadRequest? request, out ValidatedLead? lead)
    {
        var details = new List<string>();
        request ??= new CreateLeadRequest();

        var name = request.FullName?.Trim() ?? "";
        if (name.Length is < 2 or > 200)
        {
            details.Add("Please share your full name so we can get in touch.");
        }

        var phone = NormalizePhone(request.Phone);
        if (phone is null)
        {
            details.Add("Please share a phone number we can use to reach you.");
        }

        var email = request.Email?.Trim() ?? "";
        if (!IsEmail(email))
        {
            details.Add("Please share a valid email so we can follow up.");
        }

        if (details.Count > 0)
        {
            lead = null;
            return details;
        }

        var source = TrimTo(request.Source, 80);
        lead = new ValidatedLead(
            name,
            phone!,
            email,
            request.WhatsappConsent ?? false,
            source,
            BuildContext(request));
        return details;
    }

    internal static string? NormalizePhone(string? raw)
    {
        var trimmed = raw?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return null;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length is < 8 or > 15)
        {
            return null;
        }

        // Local 10-digit Indian mobiles are stored as +91…, not +98765…
        if (digits.Length == 10 && digits[0] is >= '6' and <= '9')
        {
            return "+91" + digits;
        }

        return "+" + digits;
    }

    private static bool IsEmail(string email)
        => email.Length is >= 3 and <= 254
           && email.Contains('@', StringComparison.Ordinal)
           && MailAddress.TryCreate(email, out var parsed)
           && parsed.Address.Equals(email, StringComparison.OrdinalIgnoreCase);

    private static string? TrimTo(string? value, int max)
    {
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return null;
        }

        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string BuildContext(CreateLeadRequest request)
    {
        var node = new JsonObject();
        Put(node, "helpType", TrimTo(request.HelpType, 80));
        Put(node, "need", TrimTo(request.Need, 200));
        Put(node, "travelWindow", TrimTo(request.TravelWindow, 80));

        if (request.Extra is not null)
        {
            foreach (var (key, value) in request.Extra)
            {
                if (ReservedContextKeys.Contains(key) || node.ContainsKey(key))
                {
                    continue;
                }

                node[key] = JsonNode.Parse(value.GetRawText());
            }
        }

        return node.ToJsonString(LeadJson.Options);
    }

    private static void Put(JsonObject node, string key, string? value)
    {
        if (value is not null)
        {
            node[key] = value;
        }
    }
}
