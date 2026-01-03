namespace FluentRegex.Tests.Integration;

using System.Text.RegularExpressions;
using FluentRegex;
using FluentRegex.Common;
using Xunit;

/// <summary>
/// Integration tests that verify complete end-to-end scenarios from pattern construction
/// through validation, optimization, and compilation to final regex execution.
/// </summary>
public class EndToEndIntegrationTests
{
    [Fact]
    public void CompleteEmailValidationFlow()
    {
        // Build pattern using fluent API - use Common.Email() for consistency
        var emailPattern = Common.Email();

        // Compile and test functionality
        var compiledRegex = emailPattern.Compile();
        Assert.Equal(RegexOptions.Compiled | RegexOptions.NonBacktracking, compiledRegex.Options);

        // Test against real email addresses
        Assert.Matches(compiledRegex, "test@example.com");
        Assert.Matches(compiledRegex, "user.name@domain.org");
        Assert.Matches(compiledRegex, "admin123@company.co.uk");

        // Test invalid emails
        Assert.DoesNotMatch(compiledRegex, "invalid.email");
        Assert.DoesNotMatch(compiledRegex, "@domain.com");
        Assert.DoesNotMatch(compiledRegex, "user@");
        Assert.DoesNotMatch(compiledRegex, "user@domain");
    }

    [Fact]
    public void CompletePhoneNumberValidationFlow()
    {
        // Build phone pattern that requires exact length - use Match for anchoring
        var phonePattern = Pattern.Match(
            Pattern
                .Text("(")
                .Optional()
                .Then(Pattern.Digit().Exactly(3))
                .Then(Pattern.Text(")").Optional())
                .Then(Pattern.OneOf(" -").Optional())
                .Then(Pattern.Digit().Exactly(3))
                .Then(Pattern.OneOf(" -").Optional())
                .Then(Pattern.Digit().Exactly(4))
        );

        // Compile and test functionality
        var compiledRegex = phonePattern.Compile();

        // Test various phone number formats
        Assert.Matches(compiledRegex, "1234567890");
        Assert.Matches(compiledRegex, "(123)4567890");
        Assert.Matches(compiledRegex, "123-456-7890");
        Assert.Matches(compiledRegex, "123 456 7890");
        Assert.Matches(compiledRegex, "(123) 456-7890");
        Assert.Matches(compiledRegex, "(123)-456-7890");

        // Test invalid formats - now these should properly fail due to anchoring
        Assert.DoesNotMatch(compiledRegex, "12345678901"); // Too long
        Assert.DoesNotMatch(compiledRegex, "123456789"); // Too short
        Assert.DoesNotMatch(compiledRegex, "abc-def-ghij"); // Non-digits
    }

    [Fact]
    public void CompleteUrlValidationFlow()
    {
        // Build simpler URL pattern using new character class methods
        var urlPattern = Pattern
            .Text("http")
            .Then(Pattern.Text("s").Optional())
            .Then("://")
            .Then(Pattern.OneOf("a-zA-Z0-9.-").OneOrMore());

        // Test with custom regex options
        var compiledRegex = urlPattern.Compile(RegexOptions.IgnoreCase);
        Assert.Equal(RegexOptions.IgnoreCase, compiledRegex.Options);

        // Test valid URLs
        Assert.Matches(compiledRegex, "http://example.com");
        Assert.Matches(compiledRegex, "https://example.com");
        Assert.Matches(compiledRegex, "https://www.example.com");
        Assert.Matches(compiledRegex, "https://api.example.com");

        // Test invalid URLs
        Assert.DoesNotMatch(compiledRegex, "ftp://example.com"); // Wrong protocol
        Assert.DoesNotMatch(compiledRegex, "example.com"); // No protocol
    }

    [Fact]
    public void CompleteNamedCaptureFlow()
    {
        // Build pattern with multiple named captures using new character class methods
        var logPattern = Pattern
            .Text("[")
            .Then(Pattern.Digit().Between(4, 4).Capture("year"))
            .Then("-")
            .Then(Pattern.Digit().Between(2, 2).Capture("month"))
            .Then("-")
            .Then(Pattern.Digit().Between(2, 2).Capture("day"))
            .Then("] ")
            .Then(Pattern.UpperLetter().OneOrMore().Capture("level"))
            .Then(": ")
            .Then(Pattern.OneOf("a-zA-Z0-9 .,!?").OneOrMore().Capture("message"));

        // Test named capture functionality
        var compiledRegex = logPattern.Compile();
        var testLog = "[2024-01-15] ERROR: Database connection failed";
        var match = compiledRegex.Match(testLog);

        Assert.True(match.Success);
        Assert.Equal("2024", match.Groups["year"].Value);
        Assert.Equal("01", match.Groups["month"].Value);
        Assert.Equal("15", match.Groups["day"].Value);
        Assert.Equal("ERROR", match.Groups["level"].Value);
        Assert.Equal("Database connection failed", match.Groups["message"].Value);
    }

    [Fact]
    public void CompleteMatchRootAnchoringFlow()
    {
        // Build anchored pattern for exact matching
        var exactPattern = Pattern.Match(
            Pattern.Text("START").Then(Pattern.Digit().Between(3, 5)).Then("END")
        );

        // Test exact matching behavior
        var compiledRegex = exactPattern.Compile();

        // Test exact matching behavior
        Assert.Matches(compiledRegex, "START123END");
        Assert.Matches(compiledRegex, "START12345END");

        // Should not match partial strings
        Assert.DoesNotMatch(compiledRegex, "PREFIX_START123END");
        Assert.DoesNotMatch(compiledRegex, "START123END_SUFFIX");
        Assert.DoesNotMatch(compiledRegex, "START12END"); // Too few digits
        Assert.DoesNotMatch(compiledRegex, "START123456END"); // Too many digits
    }

    [Fact]
    public void CompleteCommonPatternsIntegrationFlow()
    {
        // Test all common patterns compile and work
        var patterns = new[]
        {
            (Common.Email(), "test@example.com"),
            (Common.Phone(), "123-456-7890"),
            (Common.Date(), "12/25/2024"),
            (Common.Date("-"), "12-25-2024"),
        };

        foreach (var (pattern, testInput) in patterns)
        {
            var compiledRegex = pattern.Compile();
            Assert.Matches(compiledRegex, testInput);
        }
    }

    [Fact]
    public void CompleteErrorHandlingFlow()
    {
        // Test error handling for invalid patterns
        var nestedMatchPattern = new MatchRoot(new MatchRoot(Pattern.Digit()));
        var nestedResult = nestedMatchPattern.ToString();
        Assert.NotNull(nestedResult); // Should fallback to record ToString

        var stackedRepeat = new Repeat(new Repeat(Pattern.Digit(), new Exactly(2)), new Optional());
        var stackedResult = stackedRepeat.ToString();
        Assert.NotNull(stackedResult); // Should be optimized to valid regex
        Assert.NotEqual("FluentRegex.Repeat", stackedResult); // Should not be record ToString

        // Test valid patterns work correctly
        var validPattern = Pattern.Text("test").Then(Pattern.Digit().Exactly(3));
        var validCompiledRegex = validPattern.Compile();
        Assert.Matches(validCompiledRegex, "test123");
    }

    [Fact]
    public void CompleteOptimizationIntegrationFlow()
    {
        // Test optimization produces correct functional results

        // Adjacent text merging
        var adjacentTexts = Pattern
            .Text("hello")
            .Then(Pattern.Text(" "))
            .Then(Pattern.Text("world"))
            .Then(Pattern.Text("!"));

        var compiledAdjacent = adjacentTexts.Compile();
        Assert.Matches(compiledAdjacent, "hello world!");

        // Nested sequence flattening
        var nestedSequences = Pattern
            .Text("a")
            .Then(Pattern.Text("b").Then(Pattern.Text("c").Then(Pattern.Text("d"))))
            .Then(Pattern.Text("e"));

        var compiledNested = nestedSequences.Compile();
        Assert.Matches(compiledNested, "abcde");

        // Repetition count merging
        var stackedExactly = new Repeat(
            new Repeat(Pattern.Digit(), new Exactly(3)),
            new Exactly(2)
        );
        var compiledStacked = stackedExactly.Compile();
        Assert.Matches(compiledStacked, "123456");
    }

    [Fact]
    public void CompletePerformanceOptimizationFlow()
    {
        // Test performance optimizations work functionally

        // Quantifier optimization
        var inefficientPattern = Pattern.Digit().Between(1, int.MaxValue);
        var efficientRegex = inefficientPattern.Compile();
        Assert.Matches(efficientRegex, "123");

        var zeroToManyPattern = Pattern.Digit().Between(0, int.MaxValue);
        var zeroToManyRegex = zeroToManyPattern.Compile();
        Assert.Matches(zeroToManyRegex, "123");
        Assert.Matches(zeroToManyRegex, ""); // Should match zero digits

        // Grouping optimization
        var unnecessaryGrouping = Pattern.Text("hello").OneOrMore();
        var groupingRegex = unnecessaryGrouping.Compile();
        Assert.Matches(groupingRegex, "hello");
        Assert.Matches(groupingRegex, "hellohello");

        var necessaryGrouping = Pattern.Text("hello").Then(Pattern.Digit()).OneOrMore();
        var necessaryGroupingRegex = necessaryGrouping.Compile();
        Assert.Matches(necessaryGroupingRegex, "hello1hello2");

        // Default performance options
        var performancePattern = Pattern.Letter().OneOrMore().Then(Pattern.Digit().Exactly(3));
        var performanceRegex = performancePattern.Compile();
        Assert.True(performanceRegex.Options.HasFlag(RegexOptions.Compiled));
        Assert.True(performanceRegex.Options.HasFlag(RegexOptions.NonBacktracking));
        Assert.Matches(performanceRegex, "abc123");
    }

    [Fact]
    public void CompleteToStringIntegrationFlow()
    {
        // Test ToString() method works correctly
        var complexPattern = Pattern
            .Text("prefix")
            .Then(Pattern.Digit().Between(2, 4).Capture("numbers"))
            .Then(Pattern.Letter().Optional())
            .Then(Pattern.Text("suffix"));

        var toStringResult = complexPattern.ToString();
        Assert.NotEmpty(toStringResult);

        // Should handle invalid patterns gracefully in ToString
        var invalidPattern = new MatchRoot(new MatchRoot(Pattern.Digit()));
        var invalidToString = invalidPattern.ToString();
        Assert.NotNull(invalidToString);
        Assert.NotEmpty(invalidToString);
    }

    [Fact]
    public void CompleteCustomOptionsIntegrationFlow()
    {
        // Test custom regex options work correctly
        var testPattern = Pattern.Text("Hello").Then(Pattern.Digit().Exactly(3));

        // Test case-insensitive option
        var ignoreCaseRegex = testPattern.Compile(RegexOptions.IgnoreCase);
        Assert.Equal(RegexOptions.IgnoreCase, ignoreCaseRegex.Options);
        Assert.Matches(ignoreCaseRegex, "hello123");
        Assert.Matches(ignoreCaseRegex, "HELLO123");

        // Test case-sensitive (default behavior)
        var caseSensitiveRegex = testPattern.Compile(RegexOptions.None);
        Assert.Matches(caseSensitiveRegex, "Hello123");
        Assert.DoesNotMatch(caseSensitiveRegex, "hello123");

        // Test compiled option
        var compiledRegex = testPattern.Compile(RegexOptions.Compiled);
        Assert.True(compiledRegex.Options.HasFlag(RegexOptions.Compiled));
        Assert.Matches(compiledRegex, "Hello123");
    }
}
