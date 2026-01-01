namespace FluentRegex;

public abstract record Pattern
{
    public static implicit operator Pattern(string text) => new Text(text);

    public static Pattern Digit() => new Digit();
    public static Pattern Text(string value) =>
        string.IsNullOrEmpty(value) ? throw new ArgumentException("Text value cannot be null or empty", nameof(value)) : new Text(value);
    public static Pattern OneOf(string chars) =>
        string.IsNullOrEmpty(chars) ? throw new ArgumentException("Character set cannot be null or empty", nameof(chars)) : new CharSet(chars);
    public static Pattern Match(Pattern inner) =>
        inner is null ? throw new ArgumentNullException(nameof(inner)) : new MatchRoot(inner);
}

public static class PatternExtensions
{
    public static Pattern Then(this Pattern left, Pattern right) =>
        (left, right) switch
        {
            (null, _) => throw new ArgumentNullException(nameof(left)),
            (_, null) => throw new ArgumentNullException(nameof(right)),
            var (l, r) => new Sequence(l, r)
        };

    public static Pattern Exactly(this Pattern pattern, int count) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern)) :
        count < 0 ? throw new ArgumentException("Count must be non-negative", nameof(count)) :
        new Repeat(pattern, new Exactly(count));

    public static Pattern Between(this Pattern pattern, int min, int max) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern)) :
        min < 0 ? throw new ArgumentException("Min must be non-negative", nameof(min)) :
        max < min ? throw new ArgumentException("Max must be greater than or equal to min", nameof(max)) :
        new Repeat(pattern, new Between(min, max));

    public static Pattern Optional(this Pattern pattern) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern)) :
        new Repeat(pattern, new Optional());

    public static Pattern OneOrMore(this Pattern pattern) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern)) :
        new Repeat(pattern, new OneOrMore());

    public static Pattern Many(this Pattern pattern) =>
        pattern is null ? throw new ArgumentNullException(nameof(pattern)) :
        new Repeat(pattern, new Many());
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
    public static Result<T> Error(string error) => new() { IsSuccess = false, ErrorMessage = error };

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
            Repeat(Repeat(var inner, var count1), var count2) => MergeCounts(OptimizePattern(inner), count1, count2),
            Repeat(var inner, var count) => new Repeat(OptimizePattern(inner), count),
            Capture(var name, var inner) => new Capture(name, OptimizePattern(inner)),
            MatchRoot(var inner) => new MatchRoot(OptimizePattern(inner)),
            _ => pattern
        };

    private static IEnumerable<Pattern> FlattenSequence(Pattern pattern)
    {
        static IEnumerable<Pattern> FlattenRec(Pattern p) =>
            p switch
            {
                Sequence(var left, var right) => FlattenRec(left).Concat(FlattenRec(right)),
                _ => [p]
            };

        return FlattenRec(pattern);
    }

    private static Pattern MergeCounts(Pattern inner, Count count1, Count count2) =>
        (count1, count2) switch
        {
            (Exactly(var n1), Exactly(var n2)) => new Repeat(inner, new Exactly(n1 * n2)),
            (Exactly(var n), Optional) when n == 0 => new Repeat(inner, new Optional()),
            (Optional, Exactly(var n)) when n == 0 => new Repeat(inner, new Optional()),
            (Exactly(var n), OneOrMore) when n > 0 => new Repeat(inner, new OneOrMore()),
            (OneOrMore, Exactly(var n)) when n > 0 => new Repeat(inner, new OneOrMore()),
            (Exactly(var n), Many) => n > 0 ? new Repeat(inner, new Many()) : new Repeat(inner, new Many()),
            (Many, Exactly(var n)) => n > 0 ? new Repeat(inner, new Many()) : new Repeat(inner, new Many()),
            (Optional, Optional) => new Repeat(inner, new Optional()),
            (OneOrMore, OneOrMore) => new Repeat(inner, new OneOrMore()),
            (Many, Many) => new Repeat(inner, new Many()),
            (Optional, OneOrMore) => new Repeat(inner, new Many()),
            (OneOrMore, Optional) => new Repeat(inner, new Many()),
            (Optional, Many) => new Repeat(inner, new Many()),
            (Many, Optional) => new Repeat(inner, new Many()),
            (OneOrMore, Many) => new Repeat(inner, new Many()),
            (Many, OneOrMore) => new Repeat(inner, new Many()),
            _ => new Repeat(new Repeat(inner, count1), count2)
        };

    public static IEnumerable<Pattern> FilterEmptyPatterns(IEnumerable<Pattern> patterns) =>
        patterns.Where(p => p switch
        {
            CharSet { Chars: "" } => false,
            null => false,
            _ => true
        });

    public static Pattern OptimizeSequenceChain(IEnumerable<Pattern> patterns)
    {
        var optimizedPatterns = FilterEmptyPatterns(patterns)
            .Select(p => p switch
            {
                // Don't recursively optimize sequences here - they're already flattened
                Sequence => throw new InvalidOperationException("Sequences should be flattened before calling OptimizeSequenceChain"),
                Repeat(var inner, var count) => new Repeat(OptimizePattern(inner), count),
                Capture(var name, var inner) => new Capture(name, OptimizePattern(inner)),
                MatchRoot(var inner) => new MatchRoot(OptimizePattern(inner)),
                _ => p
            })
            .Aggregate(
                new List<Pattern>(),
                (acc, pattern) => pattern switch
                {
                    Text newText when acc.LastOrDefault() is Text lastText =>
                        [.. acc.Take(acc.Count - 1), new Text(lastText.Value + newText.Value)],
                    _ => [.. acc, pattern]
                }
            );

        return optimizedPatterns.Count switch
        {
            0 => throw new ArgumentException("Cannot create sequence from empty pattern list"),
            1 => optimizedPatterns[0],
            _ => BuildRightAssociativeSequence(optimizedPatterns)
        };
    }

    private static Pattern BuildRightAssociativeSequence(List<Pattern> patterns)
    {
        return patterns.Count switch
        {
            0 => throw new ArgumentException("Cannot build sequence from empty list"),
            1 => patterns[0],
            2 => new Sequence(patterns[0], patterns[1]),
            _ => new Sequence(patterns[0], BuildRightAssociativeSequence(patterns.Skip(1).ToList()))
        };
    }
}

public static class PatternValidation
{
    public static Result<Pattern> ValidatePattern(Pattern pattern) =>
        pattern switch
        {
            null => Result<Pattern>.Error("Pattern cannot be null"),
            MatchRoot { Inner: MatchRoot } => Result<Pattern>.Error("Nested Match patterns are not allowed"),
            MatchRoot { Inner: null } => Result<Pattern>.Error("Match pattern cannot contain null inner pattern"),
            Repeat { Inner: Repeat } => Result<Pattern>.Error("Stacked repetition patterns must be merged"),
            Repeat { Inner: null } => Result<Pattern>.Error("Repeat pattern cannot contain null inner pattern"),
            Sequence { Left: null } => Result<Pattern>.Error("Sequence pattern cannot contain null left pattern"),
            Sequence { Right: null } => Result<Pattern>.Error("Sequence pattern cannot contain null right pattern"),
            Capture { Inner: null } => Result<Pattern>.Error("Capture pattern cannot contain null inner pattern"),
            Capture { Name: null or "" } => Result<Pattern>.Error("Capture pattern must have a non-empty name"),
            Text { Value: null or "" } => Result<Pattern>.Error("Text pattern cannot have null or empty value"),
            CharSet { Chars: null or "" } => Result<Pattern>.Error("CharSet pattern cannot have null or empty characters"),
            _ => ValidateNestedPatterns(pattern)
        };

    private static Result<Pattern> ValidateNestedPatterns(Pattern pattern) =>
        pattern switch
        {
            Sequence(var left, var right) => ValidatePattern(left).Bind(_ => ValidatePattern(right)).Map(_ => pattern),
            Repeat(var inner, _) => ValidatePattern(inner).Map(_ => pattern),
            Capture(_, var inner) => ValidatePattern(inner).Map(_ => pattern),
            MatchRoot(var inner) => ValidatePattern(inner).Map(_ => pattern),
            _ => Result<Pattern>.Success(pattern)
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
                builder.Append(EscapeRegexSpecialCharacters(text.Value));
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

    private static void BuildSequence(Pattern left, Pattern right, System.Text.StringBuilder builder)
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
            _ => true
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
                _ => $"{{{value}}}"
            },
            Between(var min, var max) => (min, max) switch
            {
                (0, 1) => "?",
                (1, int.MaxValue) => "+",
                (0, int.MaxValue) => "*",
                _ when min == max => $"{{{min}}}",
                _ => $"{{{min},{max}}}"
            },
            Optional => "?",
            OneOrMore => "+",
            Many => "*",
            _ => throw new ArgumentException($"Unknown count type: {count.GetType()}")
        };

    private static string EscapeRegexSpecialCharacters(string text)
    {
        var specialChars = new[] { '.', '^', '$', '*', '+', '?', '(', ')', '[', ']', '{', '}', '|', '\\' };
        var builder = new System.Text.StringBuilder(text.Length * 2);

        foreach (var c in text)
        {
            if (specialChars.Contains(c))
                builder.Append('\\');
            builder.Append(c);
        }

        return builder.ToString();
    }

    private static string EscapeCharacterSetSpecialChars(string chars)
    {
        var specialChars = new[] { ']', '\\', '^', '-' };
        var builder = new System.Text.StringBuilder(chars.Length * 2);

        foreach (var c in chars)
        {
            if (specialChars.Contains(c))
                builder.Append('\\');
            builder.Append(c);
        }

        return builder.ToString();
    }
}
