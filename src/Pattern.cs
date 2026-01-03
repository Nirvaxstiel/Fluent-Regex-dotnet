namespace FluentRegex;

/// <summary>
/// Represents an immutable pattern that can be combined with other patterns to build regular expressions.
/// This is the base class for all pattern types in the FluentRegex library.
/// </summary>
public abstract record Pattern
{
    /// <summary>
    /// Implicitly converts a string to a Text pattern.
    /// </summary>
    /// <param name="text">The literal text to match.</param>
    /// <returns>A Text pattern that matches the specified literal text.</returns>
    public static implicit operator Pattern(string text) => new Text(text);

    /// <summary>
    /// Creates a pattern that matches a single digit character (\d).
    /// </summary>
    /// <returns>A pattern that matches any digit character (0-9).</returns>
    public static Pattern Digit() => new Digit();

    /// <summary>
    /// Creates a pattern that matches the specified literal text.
    /// </summary>
    /// <param name="value">The literal text to match. Cannot be null or empty.</param>
    /// <returns>A pattern that matches the specified literal text.</returns>
    /// <exception cref="ArgumentException">Thrown when value is null or empty.</exception>
    public static Pattern Text(string value) =>
        string.IsNullOrEmpty(value)
            ? throw new ArgumentException("Text value cannot be null or empty", nameof(value))
            : new Text(value);

    /// <summary>
    /// Creates a pattern that matches any character from the specified character set.
    /// </summary>
    /// <param name="chars">The characters to include in the character set. Cannot be null or empty.</param>
    /// <returns>A pattern that matches any character from the specified set.</returns>
    /// <exception cref="ArgumentException">Thrown when chars is null or empty.</exception>
    public static Pattern OneOf(string chars) =>
        string.IsNullOrEmpty(chars)
            ? throw new ArgumentException("Character set cannot be null or empty", nameof(chars))
            : new CharSet(chars);

    /// <summary>
    /// Creates a pattern that matches any letter (a-z, A-Z).
    /// </summary>
    /// <returns>A pattern that matches any alphabetic character.</returns>
    public static Pattern Letter() => new CharSet("a-zA-Z");

    /// <summary>
    /// Creates a pattern that matches any lowercase letter (a-z).
    /// </summary>
    /// <returns>A pattern that matches any lowercase alphabetic character.</returns>
    public static Pattern LowerLetter() => new CharSet("a-z");

    /// <summary>
    /// Creates a pattern that matches any uppercase letter (A-Z).
    /// </summary>
    /// <returns>A pattern that matches any uppercase alphabetic character.</returns>
    public static Pattern UpperLetter() => new CharSet("A-Z");

    /// <summary>
    /// Creates a pattern that matches any alphanumeric character (a-z, A-Z, 0-9).
    /// </summary>
    /// <returns>A pattern that matches any letter or digit.</returns>
    public static Pattern AlphaNumeric() => new CharSet("a-zA-Z0-9");

    /// <summary>
    /// Creates an anchored pattern that matches the entire input string from start to end.
    /// </summary>
    /// <param name="inner">The inner pattern to anchor. Cannot be null.</param>
    /// <returns>A pattern that matches the inner pattern anchored to the entire input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when inner is null.</exception>
    public static Pattern Match(Pattern inner) =>
        inner is null ? throw new ArgumentNullException(nameof(inner)) : new MatchRoot(inner);

    /// <summary>
    /// Returns the regex string representation of this pattern.
    /// If the pattern is invalid, falls back to the default record ToString implementation.
    /// </summary>
    /// <returns>The regex string representation of this pattern.</returns>
    public sealed override string ToString()
    {
        try
        {
            var optimized = PatternOptimization.OptimizePattern(this);
            var result = PatternValidation
                .ValidatePattern(optimized)
                .Map(RegexBuilder.BuildRegexString);

            return result.IsSuccess ? result.Value : base.ToString() ?? string.Empty;
        }
        catch
        {
            return base.ToString() ?? string.Empty;
        }
    }
}

/// <summary>
/// Extension methods for building and manipulating patterns in a fluent manner.
/// </summary>
public static class PatternExtensions
{
    /// <summary>
    /// Creates a sequence pattern that matches the left pattern followed by the right pattern.
    /// </summary>
    /// <param name="left">The first pattern to match. Cannot be null.</param>
    /// <param name="right">The second pattern to match. Cannot be null.</param>
    /// <returns>A pattern that matches the left pattern followed by the right pattern.</returns>
    /// <exception cref="ArgumentNullException">Thrown when left or right is null.</exception>
    public static Pattern Then(this Pattern left, Pattern right) =>
        (left, right) switch
        {
            (null, _) => throw new ArgumentNullException(nameof(left)),
            (_, null) => throw new ArgumentNullException(nameof(right)),
            var (l, r) => new Sequence(l, r),
        };

    /// <summary>
    /// Creates a repetition pattern that matches the pattern exactly the specified number of times.
    /// </summary>
    /// <param name="pattern">The pattern to repeat. Cannot be null.</param>
    /// <param name="count">The exact number of times to repeat. Must be non-negative.</param>
    /// <returns>A pattern that matches the input pattern exactly count times.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    /// <exception cref="ArgumentException">Thrown when count is negative.</exception>
    public static Pattern Exactly(this Pattern pattern, int count) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern))
        : count < 0 ? throw new ArgumentException("Count must be non-negative", nameof(count))
        : new Repeat(pattern, new Exactly(count));

    /// <summary>
    /// Creates a repetition pattern that matches the pattern between min and max times (inclusive).
    /// </summary>
    /// <param name="pattern">The pattern to repeat. Cannot be null.</param>
    /// <param name="min">The minimum number of repetitions. Must be non-negative.</param>
    /// <param name="max">The maximum number of repetitions. Must be greater than or equal to min.</param>
    /// <returns>A pattern that matches the input pattern between min and max times.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    /// <exception cref="ArgumentException">Thrown when min is negative or max is less than min.</exception>
    public static Pattern Between(this Pattern pattern, int min, int max) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern))
        : min < 0 ? throw new ArgumentException("Min must be non-negative", nameof(min))
        : max < min
            ? throw new ArgumentException("Max must be greater than or equal to min", nameof(max))
        : new Repeat(pattern, new Between(min, max));

    /// <summary>
    /// Creates a repetition pattern that matches the pattern zero or one time (equivalent to ?).
    /// </summary>
    /// <param name="pattern">The pattern to make optional. Cannot be null.</param>
    /// <returns>A pattern that matches the input pattern zero or one time.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    public static Pattern Optional(this Pattern pattern) =>
        pattern is null
            ? throw new ArgumentNullException(nameof(pattern))
            : new Repeat(pattern, new Optional());

    /// <summary>
    /// Creates a repetition pattern that matches the pattern one or more times (equivalent to +).
    /// </summary>
    /// <param name="pattern">The pattern to repeat. Cannot be null.</param>
    /// <returns>A pattern that matches the input pattern one or more times.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    public static Pattern OneOrMore(this Pattern pattern) =>
        pattern is null
            ? throw new ArgumentNullException(nameof(pattern))
            : new Repeat(pattern, new OneOrMore());

    /// <summary>
    /// Creates a repetition pattern that matches the pattern zero or more times (equivalent to *).
    /// </summary>
    /// <param name="pattern">The pattern to repeat. Cannot be null.</param>
    /// <returns>A pattern that matches the input pattern zero or more times.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    public static Pattern Many(this Pattern pattern) =>
        pattern is null
            ? throw new ArgumentNullException(nameof(pattern))
            : new Repeat(pattern, new Many());

    /// <summary>
    /// Creates a named capture group around the pattern.
    /// </summary>
    /// <param name="pattern">The pattern to capture. Cannot be null.</param>
    /// <param name="name">The name of the capture group. Cannot be null or empty.</param>
    /// <returns>A pattern that captures the input pattern with the specified name.</returns>
    /// <exception cref="ArgumentNullException">Thrown when pattern is null.</exception>
    /// <exception cref="ArgumentException">Thrown when name is null or empty.</exception>
    public static Pattern Capture(this Pattern pattern, string name) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern))
        : string.IsNullOrEmpty(name)
            ? throw new ArgumentException("Capture name cannot be null or empty", nameof(name))
        : new Capture(name, pattern);

    /// <summary>
    /// Compiles the pattern into a .NET Regex object with default options (Compiled | NonBacktracking).
    /// </summary>
    /// <param name="pattern">The pattern to compile.</param>
    /// <returns>A compiled Regex object with default performance options.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the pattern cannot be compiled.</exception>
    public static System.Text.RegularExpressions.Regex Compile(this Pattern pattern) =>
        pattern.Compile(
            System.Text.RegularExpressions.RegexOptions.Compiled
                | System.Text.RegularExpressions.RegexOptions.NonBacktracking
        );

    /// <summary>
    /// Compiles the pattern into a .NET Regex object with the specified options.
    /// </summary>
    /// <param name="pattern">The pattern to compile.</param>
    /// <param name="options">The regex options to apply.</param>
    /// <returns>A compiled Regex object with the specified options.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the pattern cannot be compiled.</exception>
    public static System.Text.RegularExpressions.Regex Compile(
        this Pattern pattern,
        System.Text.RegularExpressions.RegexOptions options
    )
    {
        var regexString = pattern.ToString();
        try
        {
            return new System.Text.RegularExpressions.Regex(regexString, options);
        }
        catch (System.ArgumentException ex)
        {
            throw new InvalidOperationException($"Invalid regex pattern: {ex.Message}", ex);
        }
    }
}

public abstract record Count;

public sealed record Text(string Value) : Pattern
{
    public static implicit operator Text(string value) => new(value);
}

public sealed record Digit : Pattern;

public sealed record CharSet(string Chars) : Pattern;

public sealed record Sequence(Pattern Left, Pattern Right) : Pattern;

public sealed record Repeat(Pattern Inner, Count Count) : Pattern;

public sealed record Capture(string Name, Pattern Inner) : Pattern;

public sealed record MatchRoot(Pattern Inner) : Pattern;

public sealed record Exactly(int Value) : Count
{
    public static implicit operator Exactly(int value) => new(value);
}

public sealed record Between(int Min, int Max) : Count;

public sealed record Optional : Count;

public sealed record OneOrMore : Count;

public sealed record Many : Count;

public readonly record struct Result<T>
{
    public bool IsSuccess { get; init; }
    public T Value { get; init; }
    public string ErrorMessage { get; init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };

    public static Result<T> Error(string error) =>
        new() { IsSuccess = false, ErrorMessage = error };

    public Result<U> Map<U>(Func<T, U> mapper) =>
        IsSuccess ? Result<U>.Success(mapper(Value)) : Result<U>.Error(ErrorMessage);

    public Result<U> Bind<U>(Func<T, Result<U>> binder) =>
        IsSuccess ? binder(Value) : Result<U>.Error(ErrorMessage);

    public T Match(Func<T> onSuccess, Func<string, T> onError) =>
        IsSuccess ? onSuccess() : onError(ErrorMessage);
}

public static class PatternOptimization
{
    public static Pattern OptimizePattern(Pattern pattern) =>
        pattern switch
        {
            null => throw new ArgumentNullException(nameof(pattern)),
            Sequence sequence => OptimizeSequenceChain(FlattenSequence(sequence)),
            Repeat(Repeat(var inner, var count1), var count2) => MergeCounts(
                OptimizePattern(inner),
                count1,
                count2
            ),
            Repeat(var inner, var count) => new Repeat(OptimizePattern(inner), count),
            Capture(var name, var inner) => new Capture(name, OptimizePattern(inner)),
            MatchRoot(var inner) => new MatchRoot(OptimizePattern(inner)),
            _ => pattern,
        };

    private static IEnumerable<Pattern> FlattenSequence(Pattern pattern)
    {
        static IEnumerable<Pattern> FlattenRec(Pattern p) =>
            p switch
            {
                Sequence(var left, var right) => FlattenRec(left).Concat(FlattenRec(right)),
                _ => [p],
            };

        return FlattenRec(pattern);
    }

    private static Repeat MergeCounts(Pattern inner, Count count1, Count count2) =>
        (count1, count2) switch
        {
            (Exactly(var n1), Exactly(var n2)) => new Repeat(inner, new Exactly(n1 * n2)),
            (Exactly(var n), Optional) when n == 0 => new Repeat(inner, new Optional()),
            (Optional, Exactly(var n)) when n == 0 => new Repeat(inner, new Optional()),
            (Exactly(var n), OneOrMore) when n > 0 => new Repeat(inner, new OneOrMore()),
            (OneOrMore, Exactly(var n)) when n > 0 => new Repeat(inner, new OneOrMore()),
            (Exactly(var n), Many) => n > 0
                ? new Repeat(inner, new Many())
                : new Repeat(inner, new Many()),
            (Many, Exactly(var n)) => n > 0
                ? new Repeat(inner, new Many())
                : new Repeat(inner, new Many()),
            (Optional, Optional) => new Repeat(inner, new Optional()),
            (OneOrMore, OneOrMore) => new Repeat(inner, new OneOrMore()),
            (Many, Many) => new Repeat(inner, new Many()),
            (Optional, OneOrMore) => new Repeat(inner, new Many()),
            (OneOrMore, Optional) => new Repeat(inner, new Many()),
            (Optional, Many) => new Repeat(inner, new Many()),
            (Many, Optional) => new Repeat(inner, new Many()),
            (OneOrMore, Many) => new Repeat(inner, new Many()),
            (Many, OneOrMore) => new Repeat(inner, new Many()),
            _ => new Repeat(new Repeat(inner, count1), count2),
        };

    public static IEnumerable<Pattern> FilterEmptyPatterns(IEnumerable<Pattern> patterns) =>
        patterns.Where(p =>
            p switch
            {
                CharSet { Chars: "" } => false,
                null => false,
                _ => true,
            }
        );

    public static Pattern OptimizeSequenceChain(IEnumerable<Pattern> patterns)
    {
        var optimizedPatterns = FilterEmptyPatterns(patterns)
            .Select(p =>
                p switch
                {
                    // Don't recursively optimize sequences here - they're already flattened
                    Sequence => throw new InvalidOperationException(
                        "Sequences should be flattened before calling OptimizeSequenceChain"
                    ),
                    Repeat(var inner, var count) => new Repeat(OptimizePattern(inner), count),
                    Capture(var name, var inner) => new Capture(name, OptimizePattern(inner)),
                    MatchRoot(var inner) => new MatchRoot(OptimizePattern(inner)),
                    _ => p,
                }
            )
            .Aggregate(
                new List<Pattern>(),
                (acc, pattern) =>
                    pattern switch
                    {
                        Text newText when acc.LastOrDefault() is Text lastText =>
                        [
                            .. acc.Take(acc.Count - 1),
                            new Text(lastText.Value + newText.Value),
                        ],
                        _ => [.. acc, pattern],
                    }
            );

        return optimizedPatterns.Count switch
        {
            0 => throw new ArgumentException("Cannot create sequence from empty pattern list"),
            1 => optimizedPatterns[0],
            _ => BuildRightAssociativeSequence(optimizedPatterns),
        };
    }

    private static Pattern BuildRightAssociativeSequence(List<Pattern> patterns)
    {
        return patterns.Count switch
        {
            0 => throw new ArgumentException("Cannot build sequence from empty list"),
            1 => patterns[0],
            2 => new Sequence(patterns[0], patterns[1]),
            _ => new Sequence(patterns[0], BuildRightAssociativeSequence(patterns[1..])),
        };
    }
}

public static class PatternValidation
{
    public static Result<Pattern> ValidatePattern(Pattern pattern) =>
        pattern switch
        {
            null => Result<Pattern>.Error("Pattern cannot be null"),
            MatchRoot { Inner: MatchRoot } => Result<Pattern>.Error(
                "Nested Match patterns are not allowed"
            ),
            MatchRoot { Inner: null } => Result<Pattern>.Error(
                "Match pattern cannot contain null inner pattern"
            ),
            Repeat { Inner: null } => Result<Pattern>.Error(
                "Repeat pattern cannot contain null inner pattern"
            ),
            Sequence { Left: null } => Result<Pattern>.Error(
                "Sequence pattern cannot contain null left pattern"
            ),
            Sequence { Right: null } => Result<Pattern>.Error(
                "Sequence pattern cannot contain null right pattern"
            ),
            Capture { Inner: null } => Result<Pattern>.Error(
                "Capture pattern cannot contain null inner pattern"
            ),
            Capture { Name: null or "" } => Result<Pattern>.Error(
                "Capture pattern must have a non-empty name"
            ),
            Text { Value: null or "" } => Result<Pattern>.Error(
                "Text pattern cannot have null or empty value"
            ),
            CharSet { Chars: null or "" } => Result<Pattern>.Error(
                "CharSet pattern cannot have null or empty characters"
            ),
            _ => ValidateNestedPatterns(pattern),
        };

    private static Result<Pattern> ValidateNestedPatterns(Pattern pattern) =>
        pattern switch
        {
            Sequence(var left, var right) => ValidatePattern(left)
                .Bind(_ => ValidatePattern(right))
                .Map(_ => pattern),
            Repeat(var inner, _) => ValidatePattern(inner).Map(_ => pattern),
            Capture(_, var inner) => ValidatePattern(inner).Map(_ => pattern),
            MatchRoot(var inner) => ValidatePattern(inner).Map(_ => pattern),
            _ => Result<Pattern>.Success(pattern),
        };
}

public static class RegexBuilder
{
    public static string BuildRegexString(Pattern pattern)
    {
        var builder = new System.Text.StringBuilder();
        BuildRegexStringInternal(pattern, builder);
        return builder.ToString();
    }

    private static void BuildRegexStringInternal(Pattern pattern, System.Text.StringBuilder builder)
    {
        switch (pattern)
        {
            case Text text:
                builder.Append(System.Text.RegularExpressions.Regex.Escape(text.Value));
                break;
            case Digit:
                builder.Append(@"\d");
                break;
            case CharSet charSet:
                builder.Append($"[{EscapeCharacterSetSpecialChars(charSet.Chars)}]");
                break;
            case Sequence(var left, var right):
                BuildSequence(left, right, builder);
                break;
            case Repeat(var inner, var count):
                BuildRepeat(inner, count, builder);
                break;
            case Capture(var name, var inner):
                BuildCapture(name, inner, builder);
                break;
            case MatchRoot(var inner):
                BuildMatchRoot(inner, builder);
                break;
            default:
                throw new ArgumentException($"Unknown pattern type: {pattern.GetType()}");
        }
    }

    private static void BuildSequence(
        Pattern left,
        Pattern right,
        System.Text.StringBuilder builder
    )
    {
        BuildRegexStringInternal(left, builder);
        BuildRegexStringInternal(right, builder);
    }

    private static void BuildRepeat(Pattern inner, Count count, System.Text.StringBuilder builder)
    {
        var needsGrouping = inner switch
        {
            Text => false,
            Digit => false,
            CharSet => false,
            Capture => false,
            _ => true,
        };

        if (needsGrouping)
            builder.Append("(?:");

        BuildRegexStringInternal(inner, builder);

        if (needsGrouping)
            builder.Append(')');

        builder.Append(BuildQuantifier(count));
    }

    private static void BuildCapture(string name, Pattern inner, System.Text.StringBuilder builder)
    {
        builder.Append($"(?<{name}>");
        BuildRegexStringInternal(inner, builder);
        builder.Append(')');
    }

    private static void BuildMatchRoot(Pattern inner, System.Text.StringBuilder builder)
    {
        builder.Append('^');
        BuildRegexStringInternal(inner, builder);
        builder.Append('$');
    }

    private static string BuildQuantifier(Count count) =>
        count switch
        {
            Exactly(var value) => value switch
            {
                0 => "",
                1 => "",
                _ => $"{{{value}}}",
            },
            Between(var min, var max) => (min, max) switch
            {
                (0, 1) => "?",
                (1, int.MaxValue) => "+",
                (0, int.MaxValue) => "*",
                _ when min == max => $"{{{min}}}",
                _ => $"{{{min},{max}}}",
            },
            Optional => "?",
            OneOrMore => "+",
            Many => "*",
            _ => throw new ArgumentException($"Unknown count type: {count.GetType()}"),
        };

    private static string EscapeCharacterSetSpecialChars(string chars)
    {
        var builder = new System.Text.StringBuilder(chars.Length * 2);

        for (var i = 0; i < chars.Length; i++)
        {
            var c = chars[i];

            switch (c)
            {
                case ']':
                case '\\':
                case '^':
                    builder.Append('\\');
                    builder.Append(c);
                    break;
                case '-':
                    var isValidRange =
                        i > 0
                        && i < chars.Length - 1
                        && IsValidRangeCharacter(chars[i - 1])
                        && IsValidRangeCharacter(chars[i + 1])
                        && chars[i - 1] < chars[i + 1];
                    if (!isValidRange)
                        builder.Append('\\');
                    builder.Append(c);
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString();
    }

    private static bool IsValidRangeCharacter(char c) =>
        (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
}
