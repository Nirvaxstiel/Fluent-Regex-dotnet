namespace FluentRegex.Tests;

using Xunit;

public class RealWorldTests
{
    // === REAL-WORLD SCENARIO TESTS ===
    // These test actual usage patterns that users would encounter

    [Fact]
    public void RealWorldScenario_EmailValidation()
    {
        // Real scenario: Building an email regex using fluent API
        // User writes: Pattern.Text("hello").Then("@").Then("world").Then(".com")
        Pattern emailPattern = Pattern.Text("hello").Then("@").Then("world").Then(".com");

        var optimized = PatternOptimization.OptimizePattern(emailPattern);

        // Should merge all adjacent text into single pattern
        Assert.IsType<Text>(optimized);
        Assert.Equal("hello@world.com", ((Text)optimized).Value);
    }

    [Fact]
    public void RealWorldScenario_PhoneNumber()
    {
        // Real scenario: Phone number with optional area code
        // Pattern: Pattern.Text("(").Optional().Then("123").Then(")").Optional().Then("-").Optional().Then("456").Then("-").Then("7890")
        Pattern areaCodeStart = Pattern.Text("(").Optional();
        Pattern areaCode = Pattern.Text("123");
        Pattern areaCodeEnd = Pattern.Text(")").Optional();
        Pattern separator1 = Pattern.Text("-").Optional();
        Pattern exchange = Pattern.Text("456");
        Pattern separator2 = Pattern.Text("-");
        Pattern number = Pattern.Text("7890");

        var phonePattern = areaCodeStart.Then(areaCode).Then(areaCodeEnd).Then(separator1).Then(exchange).Then(separator2).Then(number);
        var optimized = PatternOptimization.OptimizePattern(phonePattern);

        // Should have optimized structure with merged text patterns where possible
        // The key is that adjacent text patterns get merged, but patterns with different types don't
        Assert.IsType<Sequence>(optimized); // Complex structure remains

        // But let's check a simpler case that should merge
        Pattern simplePhone = Pattern.Text("123").Then("-").Then("456").Then("-").Then("7890");
        var simpleOptimized = PatternOptimization.OptimizePattern(simplePhone);
        Assert.IsType<Text>(simpleOptimized);
        Assert.Equal("123-456-7890", ((Text)simpleOptimized).Value);
    }

    [Fact]
    public void RealWorldScenario_NestedGrouping()
    {
        // Real scenario: User builds complex nested patterns
        // Like: (prefix + digit + suffix) + separator + (another group)
        Pattern group1 = Pattern.Text("prefix").Then(Pattern.Digit()).Then("suffix");
        Pattern separator = Pattern.Text("-");
        Pattern group2 = Pattern.Text("start").Then(Pattern.OneOf("abc")).Then("end");

        var complexPattern = group1.Then(separator).Then(group2);
        var optimized = PatternOptimization.OptimizePattern(complexPattern);

        // Should flatten and optimize: Sequence(Text("prefix"), Sequence(Digit(), Sequence(Text("suffix-start"), Sequence(CharSet("abc"), Text("end")))))
        Assert.IsType<Sequence>(optimized);
        var outerSeq = (Sequence)optimized;

        // First element should be merged text "prefix"
        Assert.IsType<Text>(outerSeq.Left);
        Assert.Equal("prefix", ((Text)outerSeq.Left).Value);

        // Rest should be properly structured
        Assert.IsType<Sequence>(outerSeq.Right);
    }

    [Fact]
    public void RealWorldScenario_ImplicitConversions()
    {
        // This is the KEY scenario - implicit string conversions create adjacent text patterns
        Pattern pattern1 = "hello";  // implicit conversion
        Pattern pattern2 = "world";  // implicit conversion
        var combined = pattern1.Then(pattern2);

        var optimized = PatternOptimization.OptimizePattern(combined);

        // This MUST merge into single text - this is the core value proposition
        Assert.IsType<Text>(optimized);
        Assert.Equal("helloworld", ((Text)optimized).Value);
    }

    [Fact]
    public void RealWorldScenario_ChainedBuilding()
    {
        // Real scenario: User builds pattern step by step
        Pattern pattern = Pattern.Text("start");
        pattern = pattern.Then("-");
        pattern = pattern.Then(Pattern.Digit().Exactly(3));
        pattern = pattern.Then("-");
        pattern = pattern.Then("end");

        var optimized = PatternOptimization.OptimizePattern(pattern);

        // Should optimize to: Sequence(Text("start-"), Sequence(Repeat(Digit, Exactly(3)), Text("-end")))
        Assert.IsType<Sequence>(optimized);
        var outerSeq = (Sequence)optimized;

        // Left should be merged text
        Assert.IsType<Text>(outerSeq.Left);
        Assert.Equal("start-", ((Text)outerSeq.Left).Value);

        // Right should contain the digit pattern and merged suffix
        Assert.IsType<Sequence>(outerSeq.Right);
        var rightSeq = (Sequence)outerSeq.Right;
        Assert.IsType<Repeat>(rightSeq.Left);
        Assert.IsType<Text>(rightSeq.Right);
        Assert.Equal("-end", ((Text)rightSeq.Right).Value);
    }
}