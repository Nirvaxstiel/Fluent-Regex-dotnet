namespace FluentRegex.Common;

using FluentRegex;

/// <summary>
/// Provides pre-built common regex patterns for typical validation scenarios.
/// All patterns are optimized and follow the same validation and compilation rules as custom patterns.
/// </summary>
public static class Common
{
    /// <summary>
    /// Creates a pattern for validating email addresses.
    /// Matches basic email format: alphanumeric characters, dots, underscores, and hyphens in the local part,
    /// followed by @ symbol, domain name, dot, and 2-6 letter top-level domain.
    /// </summary>
    /// <returns>A pattern that matches basic email address format.</returns>
    public static Pattern Email() =>
        Pattern
            .OneOf("a-zA-Z0-9._-")
            .OneOrMore()
            .Then("@")
            .Then(Pattern.OneOf("a-zA-Z0-9.-").OneOrMore())
            .Then(".")
            .Then(Pattern.Letter().Between(2, 6));

    /// <summary>
    /// Creates a pattern for validating US phone numbers with optional formatting.
    /// Supports formats like: 1234567890, (123)4567890, 123-456-7890, 123 456 7890, (123) 456-7890, (123)-456-7890.
    /// </summary>
    /// <returns>A pattern that matches US phone number formats with optional parentheses, spaces, and hyphens.</returns>
    public static Pattern Phone() =>
        Pattern
            .Text("(")
            .Optional()
            .Then(Pattern.Digit().Exactly(3))
            .Then(Pattern.Text(")").Optional())
            .Then(Pattern.OneOf(" -").Optional())
            .Then(Pattern.Digit().Exactly(3))
            .Then(Pattern.OneOf(" -").Optional())
            .Then(Pattern.Digit().Exactly(4));

    /// <summary>
    /// Creates a pattern for validating HTTP and HTTPS URLs.
    /// Matches URLs starting with http:// or https://, optional www., domain name, and optional path/query parameters.
    /// </summary>
    /// <returns>A pattern that matches HTTP and HTTPS URL formats.</returns>
    public static Pattern Url() =>
        Pattern
            .Text("http")
            .Then(Pattern.Text("s").Optional())
            .Then("://")
            .Then(Pattern.Text("www.").Optional())
            .Then(Pattern.OneOf("a-zA-Z0-9.-").OneOrMore())
            .Then(Pattern.OneOf("/a-zA-Z0-9._~:/?#[]@!$&'()*+,;=-").Many());

    /// <summary>
    /// Creates a pattern for validating IPv4 addresses.
    /// Matches four octets (1-3 digits each) separated by dots.
    /// Note: This pattern validates format but not numeric ranges (0-255 per octet).
    /// </summary>
    /// <returns>A pattern that matches IPv4 address format (xxx.xxx.xxx.xxx).</returns>
    public static Pattern IPv4() =>
        OctetPattern()
            .Then(".")
            .Then(OctetPattern())
            .Then(".")
            .Then(OctetPattern())
            .Then(".")
            .Then(OctetPattern());

    /// <summary>
    /// Creates a pattern for validating dates in MM/DD/YYYY format.
    /// Matches 1-2 digits for month and day, and exactly 4 digits for year, separated by forward slashes.
    /// </summary>
    /// <returns>A pattern that matches MM/DD/YYYY date format.</returns>
    public static Pattern Date() =>
        Pattern
            .Digit()
            .Between(1, 2)
            .Then("/")
            .Then(Pattern.Digit().Between(1, 2))
            .Then("/")
            .Then(Pattern.Digit().Exactly(4));

    /// <summary>
    /// Creates a pattern for validating dates with a custom separator.
    /// Matches 1-2 digits for month and day, and exactly 4 digits for year, separated by the specified separator.
    /// </summary>
    /// <param name="separator">The separator character(s) to use between date components (e.g., "-", ".", " ").</param>
    /// <returns>A pattern that matches MM{separator}DD{separator}YYYY date format.</returns>
    public static Pattern Date(string separator) =>
        Pattern
            .Digit()
            .Between(1, 2)
            .Then(separator)
            .Then(Pattern.Digit().Between(1, 2))
            .Then(separator)
            .Then(Pattern.Digit().Exactly(4));

    /// <summary>
    /// Creates a pattern for matching an IPv4 octet (1-3 digits).
    /// This is a helper method used by the IPv4 pattern.
    /// </summary>
    /// <returns>A pattern that matches 1-3 digits for an IPv4 octet.</returns>
    private static Pattern OctetPattern() => Pattern.Digit().Between(1, 3);
}
