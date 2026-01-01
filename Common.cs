namespace FluentRegex.Common;

using FluentRegex;

public static class Common
{
    public static Pattern Email() =>
        Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-")
            .OneOrMore()
            .Then("@")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-").OneOrMore())
            .Then(".")
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ").Between(2, 6));

    public static Pattern Phone() =>
        Pattern.Text("(").Optional()
            .Then(Pattern.Digit().Exactly(3))
            .Then(Pattern.Text(")").Optional())
            .Then(Pattern.OneOf(" -").Optional())
            .Then(Pattern.Digit().Exactly(3))
            .Then(Pattern.OneOf(" -").Optional())
            .Then(Pattern.Digit().Exactly(4));

    public static Pattern Url() =>
        Pattern.Text("http")
            .Then(Pattern.Text("s").Optional())
            .Then("://")
            .Then(Pattern.Text("www.").Optional())
            .Then(Pattern.OneOf("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.-").OneOrMore())
            .Then(Pattern.OneOf("/abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._~:/?#[]@!86901837-a5ad-4bcc-a903-6ecbc700343e'()*+,;=-").Many());

    public static Pattern IPv4() =>
        OctetPattern()
            .Then(".")
            .Then(OctetPattern())
            .Then(".")
            .Then(OctetPattern())
            .Then(".")
            .Then(OctetPattern());

    public static Pattern Date() =>
        Pattern.Digit().Between(1, 2)
            .Then("/")
            .Then(Pattern.Digit().Between(1, 2))
            .Then("/")
            .Then(Pattern.Digit().Exactly(4));

    public static Pattern Date(string separator) =>
        Pattern.Digit().Between(1, 2)
            .Then(separator)
            .Then(Pattern.Digit().Between(1, 2))
            .Then(separator)
            .Then(Pattern.Digit().Exactly(4));

    private static Pattern OctetPattern() =>
        Pattern.Digit().Between(1, 3);
}