namespace FluentRegex.Tests.Integration;

using System.ComponentModel.DataAnnotations;
using FluentRegex.Common;
using Xunit;

/// <summary>
/// Tests comparing FluentRegex.Common patterns with equivalent DataAnnotations validation.
/// These tests document the behavioral differences and validate our integration analysis.
/// </summary>
public class DataAnnotationsComparisonTests
{
    [Theory]
    [InlineData("test@example.com", true, true)]
    [InlineData("user.name@domain.co.uk", true, true)]
    [InlineData("invalid-email", false, false)]
    [InlineData("test@", false, false)]
    [InlineData("@domain.com", false, false)]
    [InlineData("", false, false)]
    public void EmailValidation_FluentRegexVsDataAnnotations_ShouldHaveSimilarBehavior(
        string input, bool expectedFluentRegex, bool expectedDataAnnotations)
    {
        // Arrange
        var fluentRegexPattern = Common.Email().Compile();
        var emailAttribute = new EmailAddressAttribute();

        // Act
        var fluentRegexResult = fluentRegexPattern.IsMatch(input);
        var dataAnnotationsResult = emailAttribute.IsValid(input);

        // Assert
        Assert.Equal(expectedFluentRegex, fluentRegexResult);
        Assert.Equal(expectedDataAnnotations, dataAnnotationsResult);
    }

    [Theory]
    [InlineData("1234567890", true, true)]
    [InlineData("(123) 456-7890", true, true)]
    [InlineData("123-456-7890", true, true)]
    [InlineData("invalid-phone", false, false)]
    [InlineData("123", false, true)] // DataAnnotations is more permissive than FluentRegex
    [InlineData("", false, false)]
    public void PhoneValidation_FluentRegexVsDataAnnotations_ShowsBehavioralDifferences(
        string input, bool expectedFluentRegex, bool expectedDataAnnotations)
    {
        // Arrange
        var fluentRegexPattern = Common.Phone().Compile();
        var phoneAttribute = new PhoneAttribute();

        // Act
        var fluentRegexResult = fluentRegexPattern.IsMatch(input);
        var dataAnnotationsResult = phoneAttribute.IsValid(input);

        // Assert - Document the behavioral differences
        Assert.Equal(expectedFluentRegex, fluentRegexResult);
        Assert.Equal(expectedDataAnnotations, dataAnnotationsResult);
    }

    [Theory]
    [InlineData("https://www.example.com", true, true)]
    [InlineData("http://example.com", false, true)] // FluentRegex has stricter www requirement
    [InlineData("https://example.com", false, true)] // FluentRegex has stricter www requirement  
    [InlineData("invalid-url", false, false)]
    [InlineData("www.example.com", false, false)]
    [InlineData("", false, false)]
    public void UrlValidation_FluentRegexVsDataAnnotations_ShowsBehavioralDifferences(
        string input, bool expectedFluentRegex, bool expectedDataAnnotations)
    {
        // Arrange
        var fluentRegexPattern = Common.Url().Compile();
        var urlAttribute = new UrlAttribute();

        // Act
        var fluentRegexResult = fluentRegexPattern.IsMatch(input);
        var dataAnnotationsResult = urlAttribute.IsValid(input);

        // Assert - Document the behavioral differences
        Assert.Equal(expectedFluentRegex, fluentRegexResult);
        Assert.Equal(expectedDataAnnotations, dataAnnotationsResult);
    }

    [Fact]
    public void CreditCardValidation_DataAnnotationsOnly_ShouldValidateLuhnAlgorithm()
    {
        // Arrange
        var creditCardAttribute = new CreditCardAttribute();

        // Valid test credit card numbers (Luhn algorithm compliant)
        var validCards = new[]
        {
            "4111111111111111", // Visa test number
            "5555555555554444", // MasterCard test number
            "378282246310005"   // Amex test number
        };

        var invalidCards = new[]
        {
            "1234567890123456", // Invalid Luhn
            "4111111111111112"  // Invalid Luhn (off by one)
        };

        // Act & Assert
        foreach (var card in validCards)
        {
            Assert.True(creditCardAttribute.IsValid(card), $"Valid card {card} should pass validation");
        }

        foreach (var card in invalidCards)
        {
            Assert.False(creditCardAttribute.IsValid(card), $"Invalid card {card} should fail validation");
        }
    }

    [Fact]
    public void IPv4Validation_FluentRegexOnly_ShouldValidateFormatNotRanges()
    {
        // Arrange
        var ipv4Pattern = Common.IPv4().Compile();

        // Act & Assert - Format validation only
        Assert.Matches(ipv4Pattern, "192.168.1.1");
        Assert.Matches(ipv4Pattern, "999.999.999.999"); // Invalid ranges but valid format
        Assert.DoesNotMatch(ipv4Pattern, "192.168.1");
        Assert.DoesNotMatch(ipv4Pattern, "not.an.ip.address");

        // Note: FluentRegex validates format only, not numeric ranges (0-255)
        // This is a documented limitation compared to full IP validation
    }
}