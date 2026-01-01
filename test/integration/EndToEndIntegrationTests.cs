namespace FluentRegex.Tests.Integration;

using FluentRegex;
using FluentRegex.Common;
using System.Text.RegularExpressions;
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
        // Build pattern using fluent API
        var emailPattern = Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-")
            .OneOrMore()
            .Then("@")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-").OneOrMore())
            .Then(".")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ").Between(2, 6));

        // Validate pattern structure
        var validationResult = PatternValidation.ValidatePattern(emailPattern);
        Assert.True(validationResult.IsSuccess);

        // Optimize pattern
        var optimizedPattern = PatternOptimization.OptimizePattern(emailPattern);
        Assert.NotNull(optimizedPattern);

        // Build regex string
        var regexString = RegexBuilder.BuildRegexString(optimizedPattern);
        Assert.NotEmpty(regexString);

        // Compile with default options
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
            Pattern.Text("(").Optional()
                .Then(Pattern.Digit().Exactly(3))
                .Then(Pattern.Text(")").Optional())
                .Then(Pattern.OneOf(" -").Optional())
                .Then(Pattern.Digit().Exactly(3))
                .Then(Pattern.OneOf(" -").Optional())
                .Then(Pattern.Digit().Exactly(4))
        );

        // Full validation, optimization, and compilation flow
        var validationResult = PatternValidation.ValidatePattern(phonePattern);
        Assert.True(validationResult.IsSuccess);

        var optimizedPattern = PatternOptimization.OptimizePattern(phonePattern);
        var regexString = RegexBuilder.BuildRegexString(optimizedPattern);
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
        // Build simpler URL pattern that will work correctly
        var urlPattern = Pattern.Text("http")
            .Then(Pattern.Text("s").Optional())
            .Then("://")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-").OneOrMore());

        // Complete flow with custom regex options
        var validationResult = PatternValidation.ValidatePattern(urlPattern);
        Assert.True(validationResult.IsSuccess);

        var optimizedPattern = PatternOptimization.OptimizePattern(urlPattern);
        var regexString = RegexBuilder.BuildRegexString(optimizedPattern);
        var compiledRegex = urlPattern.Compile(RegexOptions.IgnoreCase);
        Assert.Equal(RegexOptions.IgnoreCase, compiledRegex.Options);

        // Test valid URLs - use simpler test cases that match the pattern
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
        // Build pattern with multiple named captures
        var logPattern = Pattern.Text("[")
            .Then(Pattern.Digit().Between(4, 4).Capture("year"))
            .Then("-")
            .Then(Pattern.Digit().Between(2, 2).Capture("month"))
            .Then("-")
            .Then(Pattern.Digit().Between(2, 2).Capture("day"))
            .Then("] ")
            .Then(Pattern.OneOf("ABCDEFGHIJKLMNOPQRSTUVWXYZ").OneOrMore().Capture("level"))
            .Then(": ")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 .,!?").OneOrMore().Capture("message"));

        // Full validation and compilation
        var validationResult = PatternValidation.ValidatePattern(logPattern);
        Assert.True(validationResult.IsSuccess);

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
            Pattern.Text("START")
                .Then(Pattern.Digit().Between(3, 5))
                .Then("END")
        );

        // Validate, optimize, and compile
        var validationResult = PatternValidation.ValidatePattern(exactPattern);
        Assert.True(validationResult.IsSuccess);

        var optimizedPattern = PatternOptimization.OptimizePattern(exactPattern);
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
        // Test all common patterns through complete flow
        var emailPattern = Common.Email();
        var phonePattern = Common.Phone();
        var urlPattern = Common.Url();
        var ipv4Pattern = Common.IPv4();
        var datePattern = Common.Date();
        var customDatePattern = Common.Date("-");

        var patterns = new[] { emailPattern, phonePattern, urlPattern, ipv4Pattern, datePattern, customDatePattern };

        foreach (var pattern in patterns)
        {
            // Each pattern should pass validation
            var validationResult = PatternValidation.ValidatePattern(pattern);
            Assert.True(validationResult.IsSuccess, $"Pattern validation failed for {pattern.GetType().Name}");

            // Each pattern should optimize without errors
            var optimizedPattern = PatternOptimization.OptimizePattern(pattern);
            Assert.NotNull(optimizedPattern);

            // Each pattern should build to a valid regex string
            var regexString = RegexBuilder.BuildRegexString(optimizedPattern);
            Assert.NotEmpty(regexString);

            // Each pattern should compile successfully
            var compiledRegex = pattern.Compile();
            Assert.NotNull(compiledRegex);

            // Regex should be valid (no exceptions when creating new Regex)
            var testRegex = new Regex(regexString);
            Assert.NotNull(testRegex);
        }
    }

    [Fact]
    public void CompleteErrorHandlingFlow()
    {
        // Test complete error handling through the entire pipeline

        // 1. Invalid pattern construction should be caught by validation
        var nestedMatchPattern = new MatchRoot(new MatchRoot(Pattern.Digit()));
        var nestedValidation = PatternValidation.ValidatePattern(nestedMatchPattern);
        Assert.False(nestedValidation.IsSuccess);
        Assert.Contains("Nested Match patterns are not allowed", nestedValidation.ErrorMessage);

        // Build should throw InvalidOperationException for invalid patterns
        Assert.Throws<InvalidOperationException>(() => nestedMatchPattern.Build());

        // 2. Stacked repetition should be caught
        var stackedRepeat = new Repeat(new Repeat(Pattern.Digit(), new Exactly(2)), new Optional());
        var stackedValidation = PatternValidation.ValidatePattern(stackedRepeat);
        Assert.False(stackedValidation.IsSuccess);
        Assert.Contains("Stacked repetition patterns must be merged", stackedValidation.ErrorMessage);

        Assert.Throws<InvalidOperationException>(() => stackedRepeat.Build());

        // 3. Valid patterns should flow through successfully
        var validPattern = Pattern.Text("test").Then(Pattern.Digit().Exactly(3));
        var validValidation = PatternValidation.ValidatePattern(validPattern);
        Assert.True(validValidation.IsSuccess);

        var validRegexString = validPattern.Build();
        Assert.Equal("test\\d{3}", validRegexString);

        var validCompiledRegex = validPattern.Compile();
        Assert.Matches(validCompiledRegex, "test123");
    }

    [Fact]
    public void CompleteOptimizationIntegrationFlow()
    {
        // Test optimization through complete pipeline with complex patterns

        // 1. Adjacent text merging
        var adjacentTexts = Pattern.Text("hello")
            .Then(Pattern.Text(" "))
            .Then(Pattern.Text("world"))
            .Then(Pattern.Text("!"));

        var optimizedAdjacent = PatternOptimization.OptimizePattern(adjacentTexts);
        Assert.IsType<Text>(optimizedAdjacent);
        Assert.Equal("hello world!", ((Text)optimizedAdjacent).Value);

        var compiledAdjacent = adjacentTexts.Compile();
        Assert.Matches(compiledAdjacent, "hello world!");

        // 2. Nested sequence flattening
        var nestedSequences = Pattern.Text("a")
            .Then(Pattern.Text("b").Then(Pattern.Text("c").Then(Pattern.Text("d"))))
            .Then(Pattern.Text("e"));

        var optimizedNested = PatternOptimization.OptimizePattern(nestedSequences);
        Assert.IsType<Text>(optimizedNested);
        Assert.Equal("abcde", ((Text)optimizedNested).Value);

        var compiledNested = nestedSequences.Compile();
        Assert.Matches(compiledNested, "abcde");

        // 3. Repetition count merging - test the optimized result directly
        var stackedExactly = new Repeat(new Repeat(Pattern.Digit(), new Exactly(3)), new Exactly(2));
        var optimizedStacked = PatternOptimization.OptimizePattern(stackedExactly);
        Assert.IsType<Repeat>(optimizedStacked);
        var optimizedRepeat = (Repeat)optimizedStacked;
        Assert.IsType<Exactly>(optimizedRepeat.Count);
        Assert.Equal(6, ((Exactly)optimizedRepeat.Count).Value);

        // Build the optimized pattern instead of the original stacked one
        var compiledStacked = optimizedStacked.Build();
        Assert.Equal("\\d{6}", compiledStacked);
    }

    [Fact]
    public void CompletePerformanceOptimizationFlow()
    {
        // Test that the complete flow produces efficient regex patterns

        // 1. Quantifier optimization
        var inefficientPattern = Pattern.Digit().Between(1, int.MaxValue);
        var efficientRegex = inefficientPattern.Build();
        Assert.Equal("\\d+", efficientRegex); // Should optimize to +

        var zeroToManyPattern = Pattern.Digit().Between(0, int.MaxValue);
        var zeroToManyRegex = zeroToManyPattern.Build();
        Assert.Equal("\\d*", zeroToManyRegex); // Should optimize to *

        // 2. Grouping optimization
        var unnecessaryGrouping = Pattern.Text("hello").OneOrMore();
        var groupingRegex = unnecessaryGrouping.Build();
        Assert.Equal("hello+", groupingRegex); // No grouping needed for single text

        var necessaryGrouping = Pattern.Text("hello").Then(Pattern.Digit()).OneOrMore();
        var necessaryGroupingRegex = necessaryGrouping.Build();
        Assert.Equal("(?:hello\\d)+", necessaryGroupingRegex); // Grouping needed for sequence

        // 3. Compilation with performance options
        var performancePattern = Pattern.OneOf("abc").OneOrMore().Then(Pattern.Digit().Exactly(3));
        var performanceRegex = performancePattern.Compile();

        // Should have performance-optimized options by default
        Assert.True(performanceRegex.Options.HasFlag(RegexOptions.Compiled));
        Assert.True(performanceRegex.Options.HasFlag(RegexOptions.NonBacktracking));

        // Test actual performance characteristics
        var testString = "aaabbbccc123";
        var match = performanceRegex.Match(testString);
        Assert.True(match.Success);
        Assert.Equal(testString, match.Value);
    }

    [Fact]
    public void CompleteToStringIntegrationFlow()
    {
        // Test ToString() method integration with the complete pipeline
        var complexPattern = Pattern.Text("prefix")
            .Then(Pattern.Digit().Between(2, 4).Capture("numbers"))
            .Then(Pattern.OneOf("abc").Optional())
            .Then(Pattern.Text("suffix"));

        // ToString should use Build() internally
        var toStringResult = complexPattern.ToString();
        var buildResult = complexPattern.Build();
        Assert.Equal(buildResult, toStringResult);

        // Should handle invalid patterns gracefully in ToString
        var invalidPattern = new MatchRoot(new MatchRoot(Pattern.Digit()));
        var invalidToString = invalidPattern.ToString();
        Assert.NotNull(invalidToString);
        Assert.NotEmpty(invalidToString);
        // Should not throw, should fall back to default record ToString
    }

    [Fact]
    public void CompleteCustomOptionsIntegrationFlow()
    {
        // Test complete flow with various custom regex options
        var testPattern = Pattern.Text("Hello").Then(Pattern.Digit().Exactly(3));

        // Test different option combinations
        var options = new[]
        {
            RegexOptions.None,
            RegexOptions.IgnoreCase,
            RegexOptions.Multiline,
            RegexOptions.Singleline,
            RegexOptions.IgnoreCase | RegexOptions.Multiline,
            RegexOptions.Compiled,
            RegexOptions.ExplicitCapture
        };

        foreach (var option in options)
        {
            var compiledRegex = testPattern.Compile(option);
            Assert.Equal(option, compiledRegex.Options);

            // Should still match correctly regardless of options
            if (option.HasFlag(RegexOptions.IgnoreCase))
            {
                Assert.Matches(compiledRegex, "hello123");
                Assert.Matches(compiledRegex, "HELLO123");
            }
            else
            {
                Assert.Matches(compiledRegex, "Hello123");
                Assert.DoesNotMatch(compiledRegex, "hello123");
            }
        }
    }
}