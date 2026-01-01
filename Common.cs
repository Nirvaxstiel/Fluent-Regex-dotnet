namespace FluentRegex.Common;

using FluentRegex;

public static class Common
{
    /// <summary>
    /// Creates a pattern for basic email validation.
    /// Matches: local-part@domain.tld format
    /// </summary>
    public static Pattern Email() =>
        Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-")
            .OneOrMore()
            .Then("@")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-").OneOrMore())
            .Then(".")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ").Between(2, 6));

    /// <summary>
    /// Creates a pattern for US phone numbers with optional formatting.
    /// Matches: (123) 456-7890, 123-456-7890, 1234567890
    /// </summary>
    public static Pattern Phone() =>
        Pattern.Text("(").Optional()
            .Then(Pattern.Digit().Exactly(3))
            .Then(Pattern.Text(")").Optional())
            .Then(Pattern.OneOf(" -").Optional())
            .Then(Pattern.Digit().Exactly(3))
            .Then(Pattern.OneOf(" -").Optional())
            .Then(Pattern.Digit().Exactly(4));

    /// <summary>
    /// Creates a pattern for basic URL validation.
    /// Matches: http://example.com, https://www.example.com/path
    /// </summary>
    public static Pattern Url() =>
        Pattern.Text("http")
            .Then(Pattern.Text("s").Optional())
            .Then("://")
            .Then(Pattern.Text("www.").Optional())
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-").OneOrMore())
            .Then(Pattern.OneOf("/abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._~:/?#[]@!$&'()*+,;=-").Many());

    /// <summary>
    /// Creates a pattern for IPv4 addresses.
    /// Matches: 192.168.1.1, 10.0.0.1
    /// </summary>
    public static Pattern IPv4() =>
        OctetPattern()
            .Then(".")
            .Then(OctetPattern())
            .Then(".")
            .Then(OctetPattern())
            .Then(".")
            .Then(OctetPattern());

    /// <summary>
    /// Creates a pattern for dates in MM/DD/YYYY format.
    /// Matches: 12/31/2023, 01/01/2024
    /// </summary>
    public static Pattern Date() =>
        Pattern.Digit().Between(1, 2)  // Month
            .Then("/")
            .Then(Pattern.Digit().Between(1, 2))  // Day
            .Then("/")
            .Then(Pattern.Digit().Exactly(4));  // Year

    /// <summary>
    /// Creates a pattern for dates with configurable separator.
    /// Matches: 12-31-2023, 01.01.2024, etc.
    /// </summary>
    public static Pattern Date(string separator) =>
        Pattern.Digit().Between(1, 2)  // Month
            .Then(separator)
            .Then(Pattern.Digit().Between(1, 2))  // Day
            .Then(separator)
            .Then(Pattern.Digit().Exactly(4));  // Year

    // Helper method for IPv4 octet (0-255)
    private static Pattern OctetPattern() =>
        // This is a simplified version - matches 1-3 digits
        // A more precise version would validate 0-255 range
        Pattern.Digit().Between(1, 3);
}