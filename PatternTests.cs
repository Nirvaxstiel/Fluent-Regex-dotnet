namespace FluentRegex.Tests;

using CsCheck;
using Xunit;

public class PatternTests
{
    [Fact]
    public void ImmutablePatternConstruction() =>
        Check.Sample(
            Gen.String[1, 50],
            text =>
            {
                Text originalText = text;
                var sequenceWithText = new Sequence(originalText, new Digit());

                Assert.Equal(text, originalText.Value);
                Assert.False(ReferenceEquals(originalText, sequenceWithText));

                var originalDigit = new Digit();
                Pattern sequenceWithDigit = new Sequence(originalDigit, "test");
                Assert.False(ReferenceEquals(originalDigit, sequenceWithDigit));

                var originalCharSet = new CharSet("abc");
                var sequenceWithCharSet = new Sequence(originalCharSet, new Digit());
                Assert.Equal("abc", originalCharSet.Chars);
                Assert.False(ReferenceEquals(originalCharSet, sequenceWithCharSet));

                Pattern originalSequence = new Sequence("hello", new Digit());
                var nestedSequence = new Sequence(originalSequence, "world");
                Assert.IsType<Text>(((Sequence)originalSequence).Left);
                Assert.IsType<Digit>(((Sequence)originalSequence).Right);
                Assert.False(ReferenceEquals(originalSequence, nestedSequence));

                Exactly exactlyThree = 3;
                var originalRepeat = new Repeat(new Digit(), exactlyThree);
                var sequenceWithRepeat = new Sequence(originalRepeat, "end");
                Assert.IsType<Digit>(originalRepeat.Inner);
                Assert.IsType<Exactly>(originalRepeat.Count);
                Assert.False(ReferenceEquals(originalRepeat, sequenceWithRepeat));

                var originalCapture = new Capture("test", new Digit());
                Pattern sequenceWithCapture = new Sequence(originalCapture, "suffix");
                Assert.Equal("test", originalCapture.Name);
                Assert.IsType<Digit>(originalCapture.Inner);
                Assert.False(ReferenceEquals(originalCapture, sequenceWithCapture));

                Pattern originalMatch = new MatchRoot("pattern");
                var nestedMatch = new Sequence("prefix", originalMatch);
                Assert.IsType<Text>(((MatchRoot)originalMatch).Inner);
                Assert.False(ReferenceEquals(originalMatch, nestedMatch));

                Exactly originalExactly = 5;
                var repeatWithExactly = new Repeat(new Digit(), originalExactly);
                Assert.Equal(5, originalExactly.Value);

                var originalBetween = new Between(2, 8);
                Pattern repeatWithBetween = new Repeat("x", originalBetween);
                Assert.Equal(2, originalBetween.Min);
                Assert.Equal(8, originalBetween.Max);

                return true;
            }
        );

    [Fact]
    public void FactoryFunctionCorrectness() =>
        Check.Sample(
            from text in Gen.String[1, 50].Where(s => !string.IsNullOrEmpty(s))
            from chars in Gen.String[1, 10].Where(s => !string.IsNullOrEmpty(s))
            select (text, chars),
            data =>
            {
                var (text, chars) = data;

                var digit = Pattern.Digit();
                Assert.IsType<Digit>(digit);

                var textPattern = Pattern.Text(text);
                Assert.IsType<Text>(textPattern);
                Assert.Equal(text, ((Text)textPattern).Value);

                var charSet = Pattern.OneOf(chars);
                Assert.IsType<CharSet>(charSet);
                Assert.Equal(chars, ((CharSet)charSet).Chars);

                var innerPattern = Pattern.Digit();
                var matchRoot = Pattern.Match(innerPattern);
                Assert.IsType<MatchRoot>(matchRoot);
                Assert.Equal(innerPattern, ((MatchRoot)matchRoot).Inner);

                var sequence = textPattern.Then(digit);
                Assert.IsType<Sequence>(sequence);
                Assert.Equal(textPattern, ((Sequence)sequence).Left);
                Assert.Equal(digit, ((Sequence)sequence).Right);

                return true;
            }
        );

    [Fact]
    public void RepetitionExtensionMethods() =>
        Check.Sample(
            from count in Gen.Int[0, 100]
            from min in Gen.Int[0, 50]
            from maxOffset in Gen.Int[0, 50]
            let max = min + maxOffset
            select (count, min, max),
            data =>
            {
                var (count, min, max) = data;
                var basePattern = Pattern.Digit();

                var exactlyPattern = basePattern.Exactly(count);
                Assert.IsType<Repeat>(exactlyPattern);
                var exactlyRepeat = (Repeat)exactlyPattern;
                Assert.Equal(basePattern, exactlyRepeat.Inner);
                Assert.IsType<Exactly>(exactlyRepeat.Count);
                Assert.Equal(count, ((Exactly)exactlyRepeat.Count).Value);

                var betweenPattern = basePattern.Between(min, max);
                Assert.IsType<Repeat>(betweenPattern);
                var betweenRepeat = (Repeat)betweenPattern;
                Assert.Equal(basePattern, betweenRepeat.Inner);
                Assert.IsType<Between>(betweenRepeat.Count);
                var betweenCount = (Between)betweenRepeat.Count;
                Assert.Equal(min, betweenCount.Min);
                Assert.Equal(max, betweenCount.Max);

                var optionalPattern = basePattern.Optional();
                Assert.IsType<Repeat>(optionalPattern);
                var optionalRepeat = (Repeat)optionalPattern;
                Assert.Equal(basePattern, optionalRepeat.Inner);
                Assert.IsType<Optional>(optionalRepeat.Count);

                var oneOrMorePattern = basePattern.OneOrMore();
                Assert.IsType<Repeat>(oneOrMorePattern);
                var oneOrMoreRepeat = (Repeat)oneOrMorePattern;
                Assert.Equal(basePattern, oneOrMoreRepeat.Inner);
                Assert.IsType<OneOrMore>(oneOrMoreRepeat.Count);

                var manyPattern = basePattern.Many();
                Assert.IsType<Repeat>(manyPattern);
                var manyRepeat = (Repeat)manyPattern;
                Assert.Equal(basePattern, manyRepeat.Inner);
                Assert.IsType<Many>(manyRepeat.Count);

                Assert.False(ReferenceEquals(basePattern, exactlyPattern));
                Assert.False(ReferenceEquals(basePattern, betweenPattern));
                Assert.False(ReferenceEquals(basePattern, optionalPattern));
                Assert.False(ReferenceEquals(basePattern, oneOrMorePattern));
                Assert.False(ReferenceEquals(basePattern, manyPattern));

                return true;
            }
        );

    [Fact]
    public void ValidationErrorHandling() =>
        Check.Sample(
            from text in Gen.String[1, 50].Where(s => !string.IsNullOrEmpty(s))
            from name in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            select (text, name),
            data =>
            {
                var (text, name) = data;

                var innerMatch = new MatchRoot(Pattern.Digit());
                var nestedMatch = new MatchRoot(innerMatch);
                var nestedResult = PatternValidation.ValidatePattern(nestedMatch);
                Assert.False(nestedResult.IsSuccess);
                Assert.Contains("Nested Match patterns are not allowed", nestedResult.ErrorMessage);

                var innerRepeat = new Repeat(Pattern.Digit(), new Exactly(2));
                var stackedRepeat = new Repeat(innerRepeat, new Optional());
                var stackedResult = PatternValidation.ValidatePattern(stackedRepeat);
                Assert.False(stackedResult.IsSuccess);
                Assert.Contains("Stacked repetition patterns must be merged", stackedResult.ErrorMessage);

                var validPattern = Pattern.Text(text).Then(Pattern.Digit()).Optional();
                var validResult = PatternValidation.ValidatePattern(validPattern);
                Assert.True(validResult.IsSuccess);
                Assert.Equal(validPattern, validResult.Value);

                var complexPattern = new Capture(
                    name,
                    Pattern.OneOf("abc").Then(Pattern.Digit().Exactly(3))
                );
                var complexResult = PatternValidation.ValidatePattern(complexPattern);
                Assert.True(complexResult.IsSuccess);
                Assert.Equal(complexPattern, complexResult.Value);

                var matchPattern = Pattern.Match(Pattern.Text(text).OneOrMore());
                var matchResult = PatternValidation.ValidatePattern(matchPattern);
                Assert.True(matchResult.IsSuccess);
                Assert.Equal(matchPattern, matchResult.Value);

                return true;
            }
        );

    [Fact]
    public void SimpleTextMerging()
    {
        var text1 = new Text("hello");
        var text2 = new Text("world");
        var sequence = new Sequence(text1, text2);

        var optimized = PatternOptimization.OptimizePattern(sequence);

        Assert.IsType<Text>(optimized);
        var mergedText = (Text)optimized;
        Assert.Equal("helloworld", mergedText.Value);
    }

    [Fact]
    public void NestedSequenceFlattening()
    {
        var text1 = new Text("hello");
        var digit = new Digit();
        var text2 = new Text("world");

        var innerSequence = new Sequence(text1, digit);
        var outerSequence = new Sequence(innerSequence, text2);

        var optimized = PatternOptimization.OptimizePattern(outerSequence);

        Assert.IsType<Sequence>(optimized);
        var outerSeq = (Sequence)optimized;

        Assert.IsType<Text>(outerSeq.Left);
        Assert.Equal("hello", ((Text)outerSeq.Left).Value);

        Assert.IsType<Sequence>(outerSeq.Right);
        var rightSeq = (Sequence)outerSeq.Right;
        Assert.IsType<Digit>(rightSeq.Left);
        Assert.IsType<Text>(rightSeq.Right);
        Assert.Equal("world", ((Text)rightSeq.Right).Value);
    }

    [Fact]
    public void PatternOptimizationProperty() =>
        Check.Sample(
            from text1 in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from text2 in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from count1 in Gen.Int[1, 10]
            from count2 in Gen.Int[1, 10]
            select (text1, text2, count1, count2),
            data =>
            {
                var (text1, text2, count1, count2) = data;

                var adjacentTexts = new Sequence(new Text(text1), new Text(text2));
                var mergedText = PatternOptimization.OptimizePattern(adjacentTexts);
                Assert.IsType<Text>(mergedText);
                Assert.Equal(text1 + text2, ((Text)mergedText).Value);

                var nestedLeft = new Sequence(new Sequence(new Text(text1), new Digit()), new Text(text2));
                var flattenedLeft = PatternOptimization.OptimizePattern(nestedLeft);
                Assert.IsType<Sequence>(flattenedLeft);
                var leftSeq = (Sequence)flattenedLeft;
                Assert.IsType<Text>(leftSeq.Left);
                Assert.IsType<Sequence>(leftSeq.Right);
                var rightSeq = (Sequence)leftSeq.Right;
                Assert.IsType<Digit>(rightSeq.Left);
                Assert.IsType<Text>(rightSeq.Right);

                var nestedRight = new Sequence(new Text(text1), new Sequence(new Digit(), new Text(text2)));
                var flattenedRight = PatternOptimization.OptimizePattern(nestedRight);
                Assert.IsType<Sequence>(flattenedRight);

                var stackedExactly = new Repeat(new Repeat(new Digit(), new Exactly(count1)), new Exactly(count2));
                var combinedExactly = PatternOptimization.OptimizePattern(stackedExactly);
                Assert.IsType<Repeat>(combinedExactly);
                var exactlyRepeat = (Repeat)combinedExactly;
                Assert.IsType<Digit>(exactlyRepeat.Inner);
                Assert.IsType<Exactly>(exactlyRepeat.Count);
                Assert.Equal(count1 * count2, ((Exactly)exactlyRepeat.Count).Value);

                var stackedOptional = new Repeat(new Repeat(new Digit(), new Optional()), new Optional());
                var combinedOptional = PatternOptimization.OptimizePattern(stackedOptional);
                Assert.IsType<Repeat>(combinedOptional);
                var optionalRepeat = (Repeat)combinedOptional;
                Assert.IsType<Digit>(optionalRepeat.Inner);
                Assert.IsType<Optional>(optionalRepeat.Count);

                var stackedOneOrMore = new Repeat(new Repeat(new Digit(), new OneOrMore()), new OneOrMore());
                var combinedOneOrMore = PatternOptimization.OptimizePattern(stackedOneOrMore);
                Assert.IsType<Repeat>(combinedOneOrMore);
                var oneOrMoreRepeat = (Repeat)combinedOneOrMore;
                Assert.IsType<Digit>(oneOrMoreRepeat.Inner);
                Assert.IsType<OneOrMore>(oneOrMoreRepeat.Count);

                var optionalThenOneOrMore = new Repeat(new Repeat(new Digit(), new Optional()), new OneOrMore());
                var combinedMany = PatternOptimization.OptimizePattern(optionalThenOneOrMore);
                Assert.IsType<Repeat>(combinedMany);
                var manyRepeat = (Repeat)combinedMany;
                Assert.IsType<Digit>(manyRepeat.Inner);
                Assert.IsType<Many>(manyRepeat.Count);

                var patterns = new Pattern[] { new Text(text1), new Digit(), new CharSet("") };
                var filtered = PatternOptimization.FilterEmptyPatterns(patterns).ToList();
                Assert.Equal(2, filtered.Count);
                Assert.Contains(filtered, p => p is Text t && t.Value == text1);
                Assert.Contains(filtered, p => p is Digit);

                var nonOptimizable = new Capture("test", new Digit());
                var preserved = PatternOptimization.OptimizePattern(nonOptimizable);
                Assert.IsType<Capture>(preserved);
                Assert.Equal("test", ((Capture)preserved).Name);
                Assert.IsType<Digit>(((Capture)preserved).Inner);

                return true;
            }
        );

    [Fact]
    public void EfficientQuantifierGeneration() =>
        Check.Sample(
            from count in Gen.Int[0, 100]
            from min in Gen.Int[0, 50]
            from maxOffset in Gen.Int[0, 50]
            let max = min + maxOffset
            select (count, min, max),
            data =>
            {
                var (count, min, max) = data;
                var basePattern = Pattern.Digit();

                var exactlyPattern = basePattern.Exactly(count);
                var exactlyRegex = RegexBuilder.BuildRegexString(exactlyPattern);

                var expectedExactly = count switch
                {
                    0 => @"\d",
                    1 => @"\d",
                    _ => $@"\d{{{count}}}"
                };

                if (count == 0)
                    expectedExactly = @"\d";

                Assert.Equal(expectedExactly, exactlyRegex);

                var betweenPattern = basePattern.Between(min, max);
                var betweenRegex = RegexBuilder.BuildRegexString(betweenPattern);

                var expectedBetween = (min, max) switch
                {
                    (0, 1) => @"\d?",
                    (1, int.MaxValue) => @"\d+",
                    (0, int.MaxValue) => @"\d*",
                    _ when min == max => $@"\d{{{min}}}",
                    _ => $@"\d{{{min},{max}}}"
                };

                Assert.Equal(expectedBetween, betweenRegex);

                var optionalPattern = basePattern.Optional();
                var optionalRegex = RegexBuilder.BuildRegexString(optionalPattern);
                Assert.Equal(@"\d?", optionalRegex);

                var oneOrMorePattern = basePattern.OneOrMore();
                var oneOrMoreRegex = RegexBuilder.BuildRegexString(oneOrMorePattern);
                Assert.Equal(@"\d+", oneOrMoreRegex);

                var manyPattern = basePattern.Many();
                var manyRegex = RegexBuilder.BuildRegexString(manyPattern);
                Assert.Equal(@"\d*", manyRegex);

                var complexPattern = basePattern.Then(Pattern.Text("test")).OneOrMore();
                var complexRegex = RegexBuilder.BuildRegexString(complexPattern);
                Assert.Equal(@"(?:\dtest)+", complexRegex);

                var textPattern = Pattern.Text("hello").Exactly(3);
                var textRegex = RegexBuilder.BuildRegexString(textPattern);
                Assert.Equal(@"hello{3}", textRegex);

                return true;
            }
        );

    [Fact]
    public void RegexBuilderBasicFunctionality()
    {
        var digitPattern = Pattern.Digit();
        var digitRegex = RegexBuilder.BuildRegexString(digitPattern);
        Assert.Equal(@"\d", digitRegex);

        var textPattern = Pattern.Text("hello");
        var textRegex = RegexBuilder.BuildRegexString(textPattern);
        Assert.Equal("hello", textRegex);

        var charSetPattern = Pattern.OneOf("abc");
        var charSetRegex = RegexBuilder.BuildRegexString(charSetPattern);
        Assert.Equal("[abc]", charSetRegex);

        var sequencePattern = Pattern.Text("hello").Then(Pattern.Digit());
        var sequenceRegex = RegexBuilder.BuildRegexString(sequencePattern);
        Assert.Equal(@"hello\d", sequenceRegex);

        var repeatPattern = Pattern.Digit().Exactly(3);
        var repeatRegex = RegexBuilder.BuildRegexString(repeatPattern);
        Assert.Equal(@"\d{3}", repeatRegex);

        var complexPattern = Pattern.Text("hello").Then(Pattern.Digit()).OneOrMore();
        var complexRegex = RegexBuilder.BuildRegexString(complexPattern);
        Assert.Equal(@"(?:hello\d)+", complexRegex);

        var matchPattern = Pattern.Match(Pattern.Text("test"));
        var matchRegex = RegexBuilder.BuildRegexString(matchPattern);
        Assert.Equal("^test$", matchRegex);

        var capturePattern = new Capture("name", Pattern.Digit().Exactly(3));
        var captureRegex = RegexBuilder.BuildRegexString(capturePattern);
        Assert.Equal(@"(?<name>\d{3})", captureRegex);

        var specialTextPattern = Pattern.Text("hello.world*");
        var specialTextRegex = RegexBuilder.BuildRegexString(specialTextPattern);
        Assert.Equal(@"hello\.world\*", specialTextRegex);
    }

    [Fact]
    public void SimpleBuildTest()
    {
        var pattern = Pattern.Text("hello");
        var result = pattern.Build();
        Assert.Equal("hello", result);

        var digitPattern = Pattern.Digit();
        var digitResult = digitPattern.Build();
        Assert.Equal(@"\d", digitResult);

        var sequencePattern = Pattern.Text("hello").Then(Pattern.Digit());
        var sequenceResult = sequencePattern.Build();
        Assert.Equal(@"hello\d", sequenceResult);

        // Test ToString() method
        var toStringResult = sequencePattern.ToString();
        Assert.Equal(@"hello\d", toStringResult);

        // Test Compile() method with default options
        var compiledRegex = sequencePattern.Compile();
        Assert.Equal(System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.NonBacktracking, compiledRegex.Options);
        Assert.Matches(compiledRegex, "hello5");
        Assert.DoesNotMatch(compiledRegex, "hello");

        // Test Compile() method with custom options
        var customRegex = sequencePattern.Compile(System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        Assert.Equal(System.Text.RegularExpressions.RegexOptions.IgnoreCase, customRegex.Options);
        Assert.Matches(customRegex, "hello5");
    }

    [Fact]
    public void DeterministicBuild() =>
        Check.Sample(
            from text in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from count in Gen.Int[1, 10]
            select (text, count),
            data =>
            {
                var (text, count) = data;

                var pattern = Pattern.Text(text).Then(Pattern.Digit().Exactly(count));

                var result1 = pattern.Build();
                var result2 = pattern.Build();

                Assert.Equal(result1, result2);

                var complexPattern = Pattern.Match(
                    Pattern.OneOf("abc")
                        .Then(Pattern.Digit().Between(2, 5))
                        .Then(Pattern.Text(text).Optional())
                );

                var complexResult1 = complexPattern.Build();
                var complexResult2 = complexPattern.Build();

                Assert.Equal(complexResult1, complexResult2);

                return true;
            }
        );

    [Fact]
    public void BuildValidation() =>
        Check.Sample(
            from text in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from count in Gen.Int[1, 10]
            select (text, count),
            data =>
            {
                var (text, count) = data;

                var validPattern = Pattern.Text(text).Then(Pattern.Digit().Exactly(count));
                var validResult = validPattern.Build();
                Assert.NotNull(validResult);
                Assert.NotEmpty(validResult);

                var nestedMatch = new MatchRoot(new MatchRoot(Pattern.Digit()));
                Assert.Throws<InvalidOperationException>(() => nestedMatch.Build());

                var stackedRepeat = new Repeat(new Repeat(Pattern.Digit(), new Exactly(2)), new Optional());
                Assert.Throws<InvalidOperationException>(() => stackedRepeat.Build());

                return true;
            }
        );

    [Fact]
    public void BuildOptimization() =>
        Check.Sample(
            from text1 in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from text2 in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            select (text1, text2),
            data =>
            {
                var (text1, text2) = data;

                var adjacentTexts = new Sequence(new Text(text1), new Text(text2));
                var mergedResult = adjacentTexts.Build();

                // The result should be the escaped version of the concatenated texts
                var expectedEscaped = RegexBuilder.BuildRegexString(new Text(text1 + text2));
                Assert.Equal(expectedEscaped, mergedResult);

                var complexPattern = Pattern.Text(text1)
                    .Then(Pattern.Digit().Exactly(3))
                    .Then(Pattern.Text(text2));
                var complexResult = complexPattern.Build();

                // Check that the result contains the escaped versions and the digit pattern
                Assert.Contains(@"\d{3}", complexResult);

                return true;
            }
        );

    [Fact]
    public void CompileWithDefaultOptions() =>
        Check.Sample(
            from text in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from count in Gen.Int[1, 10]
            select (text, count),
            data =>
            {
                var (text, count) = data;

                var pattern = Pattern.Text(text).Then(Pattern.Digit().Exactly(count));
                var compiledRegex = pattern.Compile();

                var expectedOptions = System.Text.RegularExpressions.RegexOptions.Compiled |
                                    System.Text.RegularExpressions.RegexOptions.NonBacktracking;
                Assert.Equal(expectedOptions, compiledRegex.Options);

                var testString = text + new string('5', count);
                Assert.Matches(compiledRegex, testString);

                return true;
            }
        );

    [Fact]
    public void CompileWithCustomOptions() =>
        Check.Sample(
            from text in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from count in Gen.Int[1, 10]
            select (text, count),
            data =>
            {
                var (text, count) = data;

                var pattern = Pattern.Text(text).Then(Pattern.Digit().Exactly(count));

                var customOptions1 = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                var regex1 = pattern.Compile(customOptions1);
                Assert.Equal(customOptions1, regex1.Options);

                var customOptions2 = System.Text.RegularExpressions.RegexOptions.Multiline |
                                   System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                var regex2 = pattern.Compile(customOptions2);
                Assert.Equal(customOptions2, regex2.Options);

                var customOptions3 = System.Text.RegularExpressions.RegexOptions.None;
                var regex3 = pattern.Compile(customOptions3);
                Assert.Equal(customOptions3, regex3.Options);

                return true;
            }
        );
}
