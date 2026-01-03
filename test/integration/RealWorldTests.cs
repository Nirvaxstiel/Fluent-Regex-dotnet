namespace FluentRegex.Tests;

using Xunit;

public class RealWorldTests
{
    [Fact]
    public void RealWorldScenario_EmailValidation()
    {
        Pattern emailPattern = Pattern.Text("hello").Then("@").Then("world").Then(".com");

        var optimized = PatternOptimization.OptimizePattern(emailPattern);

        Assert.IsType<Text>(optimized);
        Assert.Equal("hello@world.com", ((Text)optimized).Value);
    }

    [Fact]
    public void RealWorldScenario_PhoneNumber()
    {
        Pattern areaCodeStart = Pattern.Text("(").Optional();
        Pattern areaCode = Pattern.Text("123");
        Pattern areaCodeEnd = Pattern.Text(")").Optional();
        Pattern separator1 = Pattern.Text("-").Optional();
        Pattern exchange = Pattern.Text("456");
        Pattern separator2 = Pattern.Text("-");
        Pattern number = Pattern.Text("7890");

        var phonePattern = areaCodeStart
            .Then(areaCode)
            .Then(areaCodeEnd)
            .Then(separator1)
            .Then(exchange)
            .Then(separator2)
            .Then(number);
        var optimized = PatternOptimization.OptimizePattern(phonePattern);

        Assert.IsType<Sequence>(optimized);

        Pattern simplePhone = Pattern.Text("123").Then("-").Then("456").Then("-").Then("7890");
        var simpleOptimized = PatternOptimization.OptimizePattern(simplePhone);
        Assert.IsType<Text>(simpleOptimized);
        Assert.Equal("123-456-7890", ((Text)simpleOptimized).Value);
    }

    [Fact]
    public void RealWorldScenario_BuildIntegration()
    {
        var emailPattern = Pattern
            .OneOf("abcdefghijklmnopqrstuvwxyz")
            .OneOrMore()
            .Then("@")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyz").OneOrMore())
            .Then(".")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyz").Between(2, 4));

        // Option 1: Build to string, user creates Regex
        var regexString = emailPattern.ToString();
        var regex = new System.Text.RegularExpressions.Regex(regexString);
        Assert.Matches(regex, "test@example.com");
        Assert.DoesNotMatch(regex, "invalid-email");

        // Option 2: Compile with default options
        var compiledRegex = emailPattern.Compile();
        Assert.Equal(
            System.Text.RegularExpressions.RegexOptions.Compiled
                | System.Text.RegularExpressions.RegexOptions.NonBacktracking,
            compiledRegex.Options
        );
        Assert.Matches(compiledRegex, "test@example.com");

        // Option 3: Compile with custom options
        var customRegex = emailPattern.Compile(
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Multiline
        );
        Assert.Equal(
            System.Text.RegularExpressions.RegexOptions.IgnoreCase
                | System.Text.RegularExpressions.RegexOptions.Multiline,
            customRegex.Options
        );
        Assert.Matches(customRegex, "TEST@EXAMPLE.COM");

        var phonePattern = Pattern
            .Digit()
            .Exactly(3)
            .Then("-")
            .Then(Pattern.Digit().Exactly(3))
            .Then("-")
            .Then(Pattern.Digit().Exactly(4));

        var phoneRegexString = phonePattern.ToString();
        var phoneRegex = new System.Text.RegularExpressions.Regex(phoneRegexString);
        Assert.Matches(phoneRegex, "123-456-7890");
        Assert.DoesNotMatch(phoneRegex, "12-345-6789");

        // Test ToString() method
        Assert.Equal(regexString, emailPattern.ToString());
        Assert.Equal(phoneRegexString, phonePattern.ToString());
    }

    [Fact]
    public void RealWorldScenario_NestedGrouping()
    {
        Pattern group1 = Pattern.Text("prefix").Then(Pattern.Digit()).Then("suffix");
        Pattern separator = Pattern.Text("-");
        Pattern group2 = Pattern.Text("start").Then(Pattern.OneOf("abc")).Then("end");

        var complexPattern = group1.Then(separator).Then(group2);
        var optimized = PatternOptimization.OptimizePattern(complexPattern);

        Assert.IsType<Sequence>(optimized);
        var outerSeq = (Sequence)optimized;

        Assert.IsType<Text>(outerSeq.Left);
        Assert.Equal("prefix", ((Text)outerSeq.Left).Value);

        Assert.IsType<Sequence>(outerSeq.Right);
    }

    [Fact]
    public void RealWorldScenario_ImplicitConversions()
    {
        Pattern pattern1 = "hello";
        Pattern pattern2 = "world";
        var combined = pattern1.Then(pattern2);

        var optimized = PatternOptimization.OptimizePattern(combined);

        Assert.IsType<Text>(optimized);
        Assert.Equal("helloworld", ((Text)optimized).Value);
    }

    [Fact]
    public void RealWorldScenario_ChainedBuilding()
    {
        Pattern pattern = Pattern.Text("start");
        pattern = pattern.Then("-");
        pattern = pattern.Then(Pattern.Digit().Exactly(3));
        pattern = pattern.Then("-");
        pattern = pattern.Then("end");

        var optimized = PatternOptimization.OptimizePattern(pattern);

        Assert.IsType<Sequence>(optimized);
        var outerSeq = (Sequence)optimized;

        Assert.IsType<Text>(outerSeq.Left);
        Assert.Equal("start-", ((Text)outerSeq.Left).Value);

        Assert.IsType<Sequence>(outerSeq.Right);
        var rightSeq = (Sequence)outerSeq.Right;
        Assert.IsType<Repeat>(rightSeq.Left);
        Assert.IsType<Text>(rightSeq.Right);
        Assert.Equal("-end", ((Text)rightSeq.Right).Value);
    }
}
