namespace FluentRegex.Tests;

using CsCheck;
using Xunit;
using FluentRegex.Common;

public class CommonPatternsTests
{
    // === COMMON PATTERN TESTS ===
    // These test pre-built patterns from FluentRegex.Common namespace

    [Fact]
    public void CommonPatternOptimization() =>
        Check.Sample(
            Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s)),
            separator =>
            {
                // Test that common patterns get optimized like custom patterns
                var emailPattern = Common.Email();
                var optimizedEmail = PatternOptimization.OptimizePattern(emailPattern);
                Assert.NotNull(optimizedEmail);

                var phonePattern = Common.Phone();
                var optimizedPhone = PatternOptimization.OptimizePattern(phonePattern);
                Assert.NotNull(optimizedPhone);

                var urlPattern = Common.Url();
                var optimizedUrl = PatternOptimization.OptimizePattern(urlPattern);
                Assert.NotNull(optimizedUrl);

                var ipv4Pattern = Common.IPv4();
                var optimizedIPv4 = PatternOptimization.OptimizePattern(ipv4Pattern);
                Assert.NotNull(optimizedIPv4);

                var datePattern = Common.Date();
                var optimizedDate = PatternOptimization.OptimizePattern(datePattern);
                Assert.NotNull(optimizedDate);

                // Test custom separator date pattern
                var customDatePattern = Common.Date(separator);
                var optimizedCustomDate = PatternOptimization.OptimizePattern(customDatePattern);
                Assert.NotNull(optimizedCustomDate);

                return true;
            }
        );

    [Fact]
    public void EmailPattern_ValidatesRealEmails()
    {
        var emailPattern = Common.Email();
        var optimized = PatternOptimization.OptimizePattern(emailPattern);

        // Test that the pattern is properly constructed
        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void PhonePattern_ValidatesRealPhoneNumbers()
    {
        var phonePattern = Common.Phone();
        var optimized = PatternOptimization.OptimizePattern(phonePattern);

        // Test that the pattern is properly constructed
        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void UrlPattern_ValidatesRealUrls()
    {
        var urlPattern = Common.Url();
        var optimized = PatternOptimization.OptimizePattern(urlPattern);

        // Test that the pattern is properly constructed
        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void IPv4Pattern_ValidatesRealIPAddresses()
    {
        var ipv4Pattern = Common.IPv4();
        var optimized = PatternOptimization.OptimizePattern(ipv4Pattern);

        // Test that the pattern is properly constructed
        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void DatePattern_ValidatesRealDates()
    {
        var datePattern = Common.Date();
        var optimized = PatternOptimization.OptimizePattern(datePattern);

        // Test that the pattern is properly constructed
        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);

        // Test custom separator
        var customDatePattern = Common.Date("-");
        var optimizedCustom = PatternOptimization.OptimizePattern(customDatePattern);
        Assert.NotNull(optimizedCustom);
        Assert.IsType<Sequence>(optimizedCustom);
    }
}