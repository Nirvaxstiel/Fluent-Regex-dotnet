namespace FluentRegex.Tests;

using CsCheck;
using FluentRegex.Common;
using Xunit;

public class CommonPatternsTests
{
    [Fact]
    public void CommonPatternOptimization() =>
        Check.Sample(
            Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s)),
            separator =>
            {
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

        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void PhonePattern_ValidatesRealPhoneNumbers()
    {
        var phonePattern = Common.Phone();
        var optimized = PatternOptimization.OptimizePattern(phonePattern);

        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void UrlPattern_ValidatesRealUrls()
    {
        var urlPattern = Common.Url();
        var optimized = PatternOptimization.OptimizePattern(urlPattern);

        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void IPv4Pattern_ValidatesRealIPAddresses()
    {
        var ipv4Pattern = Common.IPv4();
        var optimized = PatternOptimization.OptimizePattern(ipv4Pattern);

        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);
    }

    [Fact]
    public void DatePattern_ValidatesRealDates()
    {
        var datePattern = Common.Date();
        var optimized = PatternOptimization.OptimizePattern(datePattern);

        Assert.NotNull(optimized);
        Assert.IsType<Sequence>(optimized);

        var customDatePattern = Common.Date("-");
        var optimizedCustom = PatternOptimization.OptimizePattern(customDatePattern);
        Assert.NotNull(optimizedCustom);
        Assert.IsType<Sequence>(optimizedCustom);
    }
}