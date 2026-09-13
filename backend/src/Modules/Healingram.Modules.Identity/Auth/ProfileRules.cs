using System.Text.RegularExpressions;

namespace Healingram.Modules.Identity.Auth;

internal static class ProfileRules
{
    private static readonly Regex NamePattern = new(
        @"^[\p{L}][\p{L} .'-]{0,59}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex EmailPattern = new(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)*\.[A-Za-z]{2,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    internal static string DisplayName(string? firstName, string? lastName, string? fullName, string email)
    {
        var combined = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
        if (combined.Length > 0)
        {
            return combined;
        }

        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName.Trim();
        }

        return email;
    }

    internal static (string? FirstName, string? LastName) SplitName(
        string? firstName,
        string? lastName,
        string? fullName)
    {
        if (!string.IsNullOrWhiteSpace(firstName) || !string.IsNullOrWhiteSpace(lastName))
        {
            return (NullIfEmpty(firstName), NullIfEmpty(lastName));
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (null, null);
        }

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1 ? (parts[0], null) : (parts[0], parts[1]);
    }

    private static string? NullIfEmpty(string? raw)
    {
        var trimmed = raw?.Trim() ?? "";
        return trimmed.Length == 0 ? null : trimmed;
    }

    internal static List<string> Validate(
        string? firstName,
        string? lastName,
        string? phone,
        string? email,
        string? address,
        string? password = null,
        string? confirmPassword = null,
        bool requirePassword = false)
    {
        var details = new List<string>();

        if (!IsName(firstName))
        {
            details.Add("firstName is required");
        }

        if (!IsName(lastName))
        {
            details.Add("lastName is required");
        }

        details.AddRange(PhoneErrors(phone));

        var normalizedEmail = email?.Trim() ?? "";
        if (normalizedEmail.Length == 0)
        {
            details.Add("email is required");
        }
        else if (!IsEmail(normalizedEmail))
        {
            details.Add("email is not valid");
        }

        if (address is { Length: > 0 } && address.Trim().Length > 200)
        {
            details.Add("address must be 200 characters or fewer");
        }

        if (requirePassword)
        {
            if (!IsStrongPassword(password))
            {
                details.Add(
                    "password must be at least 8 characters and include a letter, a number, and a special character");
            }

            if (password != confirmPassword)
            {
                details.Add("passwords do not match");
            }
        }

        return details;
    }

    internal const string IndiaCountryCode = "+91";

    internal static string? NormalizePhone(string? raw)
    {
        var national = NationalDigits(raw);
        if (national.Length == 10 && national[0] is >= '6' and <= '9')
        {
            return IndiaCountryCode + national;
        }

        return null;
    }

    private static IEnumerable<string> PhoneErrors(string? raw)
    {
        var national = NationalDigits(raw);
        if (national.Length != 10)
        {
            yield return "phone must be exactly 10 digits";
            yield break;
        }

        if (national[0] is < '6' or > '9')
        {
            yield return "phone must start with 6, 7, 8, or 9";
        }
    }

    private static string NationalDigits(string? raw)
    {
        var digits = new string((raw ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
        {
            return digits[2..];
        }

        return digits;
    }

    private static bool IsStrongPassword(string? password)
        => !string.IsNullOrEmpty(password)
           && password.Length >= 8
           && password.Any(char.IsLetter)
           && password.Any(char.IsDigit)
           && password.Any(ch => !char.IsLetterOrDigit(ch));

    internal static string? OptionalAddress(string? raw)
    {
        var trimmed = raw?.Trim() ?? "";
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static bool IsName(string? raw)
    {
        var trimmed = raw?.Trim() ?? "";
        return trimmed.Length is >= 1 and <= 60 && NamePattern.IsMatch(trimmed);
    }

    private static bool IsEmail(string email)
        => email.Length is >= 6 and <= 254
           && !email.Contains("..", StringComparison.Ordinal)
           && EmailPattern.IsMatch(email);
}
