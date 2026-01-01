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

    [Fact]
    public void NamedCaptureSupport() =>
        Check.Sample(
            from name in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s) &&
                                                      s.All(char.IsLetterOrDigit) &&
                                                      char.IsLetter(s[0]))
            from text in Gen.String[1, 20].Where(s => !string.IsNullOrEmpty(s))
            from count in Gen.Int[1, 10]
            select (name, text, count),
            data =>
            {
                var (name, text, count) = data;

                // Test basic capture creation
                var innerPattern = Pattern.Digit().Exactly(count);
                var capturePattern = innerPattern.Capture(name);

                Assert.IsType<Capture>(capturePattern);
                var capture = (Capture)capturePattern;
                Assert.Equal(name, capture.Name);
                Assert.Equal(innerPattern, capture.Inner);

                // Test capture with complex inner pattern
                var complexInner = Pattern.Text(text).Then(Pattern.Digit()).OneOrMore();
                var complexCapture = complexInner.Capture(name);

                Assert.IsType<Capture>(complexCapture);
                var complexCaptureTyped = (Capture)complexCapture;
                Assert.Equal(name, complexCaptureTyped.Name);
                Assert.Equal(complexInner, complexCaptureTyped.Inner);

                // Test regex generation
                var regexString = RegexBuilder.BuildRegexString(capturePattern);
                var expectedRegex = count switch
                {
                    0 => $"(?<{name}>\\d)",
                    1 => $"(?<{name}>\\d)",
                    _ => $"(?<{name}>\\d{{{count}}})"
                };
                Assert.Equal(expectedRegex, regexString);

                // Test compilation and matching
                var compiledRegex = capturePattern.Compile();
                var testString = new string('5', count);
                var match = compiledRegex.Match(testString);
                Assert.True(match.Success);
                Assert.True(match.Groups.ContainsKey(name));
                Assert.Equal(testString, match.Groups[name].Value);

                return true;
            }
        );

    // Edge Case Tests

    [Fact]
    public void EmptyStringInputHandling()
    {
        // Test that empty strings are properly rejected
        Assert.Throws<ArgumentException>(() => Pattern.Text(""));
        Assert.Throws<ArgumentException>(() => Pattern.OneOf(""));

        // Test null handling
        Assert.Throws<ArgumentException>(() => Pattern.Text(null!));
        Assert.Throws<ArgumentException>(() => Pattern.OneOf(null!));
        Assert.Throws<ArgumentNullException>(() => Pattern.Match(null!));

        // Test extension method null handling
        Assert.Throws<ArgumentNullException>(() => ((Pattern)null!).Then(Pattern.Digit()));
        Assert.Throws<ArgumentNullException>(() => Pattern.Digit().Then(null!));
        Assert.Throws<ArgumentNullException>(() => ((Pattern)null!).Exactly(3));
        Assert.Throws<ArgumentNullException>(() => ((Pattern)null!).Capture("test"));
        Assert.Throws<ArgumentException>(() => Pattern.Digit().Capture(""));
        Assert.Throws<ArgumentException>(() => Pattern.Digit().Capture(null!));
    }

    [Fact]
    public void SpecialRegexCharactersInTextPatterns()
    {
        // Test all special regex characters are properly escaped
        var specialChars = new[] { '.', '^', '$', '*', '+', '?', '(', ')', '[', ']', '{', '}', '|', '\\' };

        foreach (var specialChar in specialChars)
        {
            var pattern = Pattern.Text(specialChar.ToString());
            var regexString = pattern.Build();

            // Should be escaped with backslash
            Assert.Equal($"\\{specialChar}", regexString);

            // Should compile without errors
            var regex = pattern.Compile();
            Assert.Matches(regex, specialChar.ToString());
        }

        // Test combination of special characters
        var complexSpecialText = ".*+?()[]{}|\\^$";
        var complexPattern = Pattern.Text(complexSpecialText);
        var complexRegex = complexPattern.Build();

        // All should be escaped
        Assert.Equal("\\.\\*\\+\\?\\(\\)\\[\\]\\{\\}\\|\\\\\\^\\$", complexRegex);

        // Should match the original text
        var compiledRegex = complexPattern.Compile();
        Assert.Matches(compiledRegex, complexSpecialText);
    }

    [Fact]
    public void CharSetSpecialCharactersHandling()
    {
        // Test character set special characters are properly escaped
        var charSetSpecialChars = new[] { ']', '\\', '^', '-' };

        foreach (var specialChar in charSetSpecialChars)
        {
            var pattern = Pattern.OneOf(specialChar.ToString());
            var regexString = pattern.Build();

            // Should be escaped within character set
            Assert.Equal($"[\\{specialChar}]", regexString);

            // Should compile and match
            var regex = pattern.Compile();
            Assert.Matches(regex, specialChar.ToString());
        }

        // Test normal characters in character sets
        var normalChars = "abc123";
        var normalPattern = Pattern.OneOf(normalChars);
        var normalRegex = normalPattern.Build();
        Assert.Equal("[abc123]", normalRegex);
    }

    [Fact]
    public void BoundaryConditionsForRepetitionCounts()
    {
        var basePattern = Pattern.Digit();

        // Test zero count
        var zeroPattern = basePattern.Exactly(0);
        var zeroRegex = zeroPattern.Build();
        Assert.Equal("\\d", zeroRegex); // Zero count should result in no quantifier

        // Test one count
        var onePattern = basePattern.Exactly(1);
        var oneRegex = onePattern.Build();
        Assert.Equal("\\d", oneRegex); // One count should result in no quantifier

        // Test large count
        var largePattern = basePattern.Exactly(1000);
        var largeRegex = largePattern.Build();
        Assert.Equal("\\d{1000}", largeRegex);

        // Test Between with same min/max
        var sameMinMaxPattern = basePattern.Between(5, 5);
        var sameMinMaxRegex = sameMinMaxPattern.Build();
        Assert.Equal("\\d{5}", sameMinMaxRegex);

        // Test Between with zero min
        var zeroMinPattern = basePattern.Between(0, 5);
        var zeroMinRegex = zeroMinPattern.Build();
        Assert.Equal("\\d{0,5}", zeroMinRegex);

        // Test Between with large range
        var largeRangePattern = basePattern.Between(100, 200);
        var largeRangeRegex = largeRangePattern.Build();
        Assert.Equal("\\d{100,200}", largeRangeRegex);

        // Test negative counts are rejected
        Assert.Throws<ArgumentException>(() => basePattern.Exactly(-1));
        Assert.Throws<ArgumentException>(() => basePattern.Between(-1, 5));
        Assert.Throws<ArgumentException>(() => basePattern.Between(5, 3)); // max < min
    }

    [Fact]
    public void ComplexNestedPatternCombinations()
    {
        // Test deeply nested sequences
        var deeplyNested = Pattern.Text("a")
            .Then(Pattern.Text("b").Then(Pattern.Text("c").Then(Pattern.Text("d"))))
            .Then(Pattern.Text("e"));

        var optimized = PatternOptimization.OptimizePattern(deeplyNested);
        Assert.IsType<Text>(optimized);
        Assert.Equal("abcde", ((Text)optimized).Value);

        // Test nested repetitions with different counts
        var nestedRepetitions = Pattern.Digit()
            .Exactly(3)
            .Then(Pattern.Text("x").Optional())
            .Then(Pattern.OneOf("abc").OneOrMore());

        var nestedRegex = nestedRepetitions.Build();
        Assert.Equal("\\d{3}x?[abc]+", nestedRegex);

        // Test complex capture nesting
        var complexCapture = Pattern.Text("start")
            .Then(Pattern.Digit().Exactly(3).Capture("numbers"))
            .Then(Pattern.OneOf("abc").OneOrMore().Capture("letters"))
            .Then(Pattern.Text("end"));

        var captureRegex = complexCapture.Build();
        Assert.Equal("start(?<numbers>\\d{3})(?<letters>[abc]+)end", captureRegex);

        // Test Match with complex inner pattern
        var matchWithComplex = Pattern.Match(
            Pattern.Text("prefix")
                .Then(Pattern.Digit().Between(2, 4))
                .Then(Pattern.OneOf("xyz").Optional())
                .Then(Pattern.Text("suffix"))
        );

        var matchRegex = matchWithComplex.Build();
        Assert.Equal("^prefix\\d{2,4}[xyz]?suffix$", matchRegex);

        // Test multiple levels of grouping
        var multiLevel = Pattern.Text("outer")
            .Then(
                Pattern.Text("(")
                    .Then(Pattern.Digit().OneOrMore())
                    .Then(Pattern.Text(","))
                    .Then(Pattern.Digit().OneOrMore())
                    .Then(Pattern.Text(")"))
                    .Exactly(2)
            )
            .Then(Pattern.Text("end"));

        var multiLevelRegex = multiLevel.Build();
        Assert.Equal("outer(?:\\(\\d+,\\d+\\)){2}end", multiLevelRegex);
    }

    [Fact]
    public void EdgeCaseValidationScenarios()
    {
        // Test validation catches deeply nested Match patterns
        var deepMatch = new MatchRoot(new MatchRoot(new MatchRoot(Pattern.Digit())));
        var result = PatternValidation.ValidatePattern(deepMatch);
        Assert.False(result.IsSuccess);
        Assert.Contains("Nested Match patterns are not allowed", result.ErrorMessage);

        // Test validation catches deeply nested Repeat patterns
        var deepRepeat = new Repeat(new Repeat(new Repeat(Pattern.Digit(), new Exactly(2)), new Optional()), new OneOrMore());
        var repeatResult = PatternValidation.ValidatePattern(deepRepeat);
        Assert.False(repeatResult.IsSuccess);
        Assert.Contains("Stacked repetition patterns must be merged", repeatResult.ErrorMessage);

        // Test validation with null inner patterns - these should be caught by validation, not constructors
        var nullMatchRoot = new MatchRoot(null!);
        var nullMatchResult = PatternValidation.ValidatePattern(nullMatchRoot);
        Assert.False(nullMatchResult.IsSuccess);
        Assert.Contains("Match pattern cannot contain null inner pattern", nullMatchResult.ErrorMessage);

        var nullRepeat = new Repeat(null!, new Exactly(3));
        var nullRepeatResult = PatternValidation.ValidatePattern(nullRepeat);
        Assert.False(nullRepeatResult.IsSuccess);
        Assert.Contains("Repeat pattern cannot contain null inner pattern", nullRepeatResult.ErrorMessage);

        var nullCapture = new Capture("test", null!);
        var nullCaptureResult = PatternValidation.ValidatePattern(nullCapture);
        Assert.False(nullCaptureResult.IsSuccess);
        Assert.Contains("Capture pattern cannot contain null inner pattern", nullCaptureResult.ErrorMessage);

        var nullSequenceLeft = new Sequence(null!, Pattern.Digit());
        var nullSequenceLeftResult = PatternValidation.ValidatePattern(nullSequenceLeft);
        Assert.False(nullSequenceLeftResult.IsSuccess);
        Assert.Contains("Sequence pattern cannot contain null left pattern", nullSequenceLeftResult.ErrorMessage);

        var nullSequenceRight = new Sequence(Pattern.Digit(), null!);
        var nullSequenceRightResult = PatternValidation.ValidatePattern(nullSequenceRight);
        Assert.False(nullSequenceRightResult.IsSuccess);
        Assert.Contains("Sequence pattern cannot contain null right pattern", nullSequenceRightResult.ErrorMessage);
    }

    [Fact]
    public void ExtremeBoundaryConditions()
    {
        // Test with very long text patterns
        var longText = new string('a', 10000);
        var longPattern = Pattern.Text(longText);
        var longRegex = longPattern.Build();
        Assert.Equal(longText, longRegex); // No special chars, so no escaping needed

        // Test with single character patterns
        var singleChar = Pattern.Text("x");
        var singleRegex = singleChar.Build();
        Assert.Equal("x", singleRegex);

        // Test empty character set handling in optimization
        var emptyCharSet = new CharSet("");
        var patterns = new Pattern[] { Pattern.Text("hello"), emptyCharSet, Pattern.Digit() };
        var filtered = PatternOptimization.FilterEmptyPatterns(patterns).ToList();
        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(emptyCharSet, filtered);

        // Test optimization with single pattern
        var singlePatternList = new Pattern[] { Pattern.Text("single") };
        var singleOptimized = PatternOptimization.OptimizeSequenceChain(singlePatternList);
        Assert.IsType<Text>(singleOptimized);
        Assert.Equal("single", ((Text)singleOptimized).Value);

        // Test Between with int.MaxValue
        var maxValuePattern = Pattern.Digit().Between(1, int.MaxValue);
        var maxValueRegex = maxValuePattern.Build();
        Assert.Equal("\\d+", maxValueRegex); // Should optimize to +

        var zeroToMaxPattern = Pattern.Digit().Between(0, int.MaxValue);
        var zeroToMaxRegex = zeroToMaxPattern.Build();
        Assert.Equal("\\d*", zeroToMaxRegex); // Should optimize to *
    }

}