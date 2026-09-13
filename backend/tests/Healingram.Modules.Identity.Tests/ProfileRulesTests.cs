using Healingram.Modules.Identity.Auth;
using Xunit;

namespace Healingram.Modules.Identity.Tests;

public class ProfileRulesTests
{
    [Fact]
    public void Valid_signup_fields_have_no_errors()
    {
        var details = ProfileRules.Validate(
            "Rahul",
            "Sharma",
            "9876543210",
            "rahul@local.test",
            "Bengaluru",
            "Local123!",
            "Local123!",
            requirePassword: true);

        Assert.Empty(details);
        Assert.Equal("+919876543210", ProfileRules.NormalizePhone("9876543210"));
    }

    [Fact]
    public void Missing_names_phone_and_mismatched_password_are_rejected()
    {
        var details = ProfileRules.Validate(
            "",
            "",
            "123",
            "not-an-email",
            new string('x', 201),
            "short",
            "other",
            requirePassword: true);

        Assert.Contains("firstName is required", details);
        Assert.Contains("lastName is required", details);
        Assert.Contains("phone must be exactly 10 digits", details);
        Assert.Contains("email is not valid", details);
        Assert.Contains("address must be 200 characters or fewer", details);
        Assert.Contains(
            "password must be at least 8 characters and include a letter, a number, and a special character",
            details);
        Assert.Contains("passwords do not match", details);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_email_is_required(string email)
    {
        var details = ProfileRules.Validate("Rahul", "Sharma", "9876543210", email, null);

        Assert.Contains("email is required", details);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("rahul@")]
    [InlineData("@local.test")]
    [InlineData("rahul@gmail")]
    [InlineData("rahul@gmail.")]
    [InlineData("rahul..sharma@local.test")]
    public void Invalid_email_is_rejected(string email)
    {
        var details = ProfileRules.Validate("Rahul", "Sharma", "9876543210", email, null);

        Assert.Contains("email is not valid", details);
    }

    [Theory]
    [InlineData("rahul@local.test")]
    [InlineData("Rahul.Sharma+uat@gmail.com")]
    [InlineData("guest@mail.co.in")]
    public void Valid_email_is_accepted(string email)
    {
        var details = ProfileRules.Validate("Rahul", "Sharma", "9876543210", email, null);

        Assert.DoesNotContain(details, item => item.Contains("email", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("Local123")]
    [InlineData("Local!!!!")]
    [InlineData("12345678!")]
    public void Weak_password_is_rejected(string password)
    {
        var details = ProfileRules.Validate(
            "Rahul",
            "Sharma",
            "9876543210",
            "rahul@local.test",
            null,
            password,
            password,
            requirePassword: true);

        Assert.Contains(
            "password must be at least 8 characters and include a letter, a number, and a special character",
            details);
    }

    [Theory]
    [InlineData("98765")]
    [InlineData("98765432100")]
    public void Phone_must_be_exactly_ten_digits(string phone)
    {
        var details = ProfileRules.Validate("Rahul", "Sharma", phone, "rahul@local.test", null);

        Assert.Contains("phone must be exactly 10 digits", details);
    }

    [Fact]
    public void Phone_must_be_an_indian_mobile()
    {
        var details = ProfileRules.Validate("Rahul", "Sharma", "1876543210", "rahul@local.test", null);

        Assert.Contains("phone must start with 6, 7, 8, or 9", details);
    }

    [Fact]
    public void Profile_edit_does_not_require_password()
    {
        var details = ProfileRules.Validate("Priya", "Nair", "+91 9988776655", "priya@local.test", null);

        Assert.Empty(details);
        Assert.Equal("+919988776655", ProfileRules.NormalizePhone("+91 9988776655"));
    }
}
