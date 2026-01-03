# FluentRegex

A fluent API for building regular expressions in C# that you can actually read.

Instead of writing `^[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,6}$`, write this:

```csharp
var emailPattern = Pattern.OneOf("a-zA-Z0-9._-")
    .OneOrMore()
    .Then("@")
    .Then(Pattern.OneOf("a-zA-Z0-9.-").OneOrMore())
    .Then(".")
    .Then(Pattern.Letter().Between(2, 6));

var regex = emailPattern.Compile();
```

Same result, readable code.

## Usage

```csharp
using FluentRegex;

// Basic patterns
var digits = Pattern.Digit().Exactly(3);           // \d{3}
var optional = Pattern.Text("www.").Optional();    // (?:www\.)?
var choice = Pattern.OneOf("abc").OneOrMore();     // [abc]+

// Chain them together
var phonePattern = Pattern.Match(
    Pattern.Text("(").Optional()
        .Then(Pattern.Digit().Exactly(3))
        .Then(Pattern.Text(")").Optional())
        .Then(Pattern.OneOf(" -").Optional())
        .Then(Pattern.Digit().Exactly(3))
        .Then(Pattern.OneOf(" -").Optional())
        .Then(Pattern.Digit().Exactly(4))
);

// Compile to .NET Regex
var phoneRegex = phonePattern.Compile();
var isValid = phoneRegex.IsMatch("(555) 123-4567");
```

## Common Patterns

Pre-built patterns for typical validation. For most .NET apps, use DataAnnotations instead - these are here for self-contained consistency:

```csharp
using FluentRegex.Common;

var email = Common.Email();
var phone = Common.Phone();
var url = Common.Url();
var ipv4 = Common.IPv4();
var date = Common.Date();           // MM/DD/YYYY
var dateCustom = Common.Date("-");  // MM-DD-YYYY
```

## Features

- **Type Safety**: Catch pattern errors at compile time
- **Readable Code**: `Pattern.Digit().Between(2, 4)` instead of `\d{2,4}`
- **Smart Optimization**: Adjacent text merges, sequences flatten automatically
- **Named Captures**: `pattern.Capture("year")` instead of group indices
- **Performance**: Compiles to standard .NET Regex with NonBacktracking

## API Reference

### Basic Patterns

```csharp
Pattern.Text("hello")           // Literal text
Pattern.Digit()                 // \d
Pattern.Letter()                // [a-zA-Z]
Pattern.LowerLetter()           // [a-z]
Pattern.UpperLetter()           // [A-Z]
Pattern.AlphaNumeric()          // [a-zA-Z0-9]
Pattern.OneOf("abc123")         // [abc123]
Pattern.OneOf("a-z0-9")         // [a-z0-9] (supports ranges)
```

### Repetition

```csharp
pattern.Exactly(3)              // {3}
pattern.Between(2, 5)           // {2,5}
pattern.Optional()              // ?
pattern.OneOrMore()             // +
pattern.Many()                  // *
```

### Composition

```csharp
left.Then(right)                // Sequence
pattern.Capture("name")         // Named capture group
Pattern.Match(pattern)          // Anchor to full string (^...$)
```

### Compilation

```csharp
pattern.ToString()              // Get regex string
pattern.Compile()               // Get .NET Regex with default options
pattern.Compile(RegexOptions.IgnoreCase)  // Get .NET Regex with custom options
```

## Installation

```bash
dotnet add package FluentRegex
```

## Requirements

- .NET 10.0 or later
- C# 14.0 or later

## License

MIT
