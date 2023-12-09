# Devices

This is a solution containing projects that are .NET implementations of lex/flex and yacc/bison, the compiler tools.

## Lad

The scanner requires a field, `reader`, of type `TextReader`.

The scanner methods must be of the form `ReturnType? MethodName(string? pattern, string tokenValue)`.  They must return `null` if
they want the invoking method to loop instead of returning a value.

C# and Lad treat `"a\"b"` and `@"a""b"` the same.
C# and Lad treat `"\a\"\b"` and `@"\a""\b"` differently since Lad always interprets escape sequences.
	C# sees three and five characters, respectively, while Lad sees three characters in both.
Specifying an initial `$` to signal a literal string results in each of those four strings having its own interpretation.

Lad has a special escape, `\N`, that denotes a platform-specific newline.  The `\n` escape is specifically the line feed character,
ASCII value 10.

Lad has named expressions.  Create a variable, either `const` or `readonly`, that holds the named expression and use either that
variable or the equivalent named expression syntax, `{name}`, as the `case` label.  Lad issues an error if it detects use of both
the variable and the named expression.  There is no error if the named expression is part of a larger regular expression.

The `Write` method of the scanner's internal `Reader_` class adds a string to its internal buffer after the recognized token text
but before any trailing context.

Lad emits code written in section one in `%{` and `%}` blocks at the beginning of the file.  This is the place for `#define` and
`using` directives.

The generated scanner uses the return value of the user-provided scanner method to determine whether or not to return that value.
It will return it if it is not the default (`null` for class types) and loop if it is the default.  This might be problematic for
integral values since their default is `0`, which might be a valid return value.  Consider using `int?` in such cases.

### Lad Operator Precedence

Here is the precedence of operations in Lad from highest to lowest.

- literal
- group `(` `)`
- class `[` `[^` `-` `]`
- Kleene `*` `+` `{` `,` `}`
- concatenation
- choice `|`
- trailing context `/`

I considered swapping the precedence of `|` and `/`.  I won't do this since having `|` as the lowest precedence is equivalent to
specifying multiple expressions.

## Pard

If a terminal appears in the "symbols" section, emit its definition even if it doesn't appear in the "rules" section.
Allow generics in type casts.
Allow nullable object nonterminals to have an empty action.  See modification to Lad below.

- Consider having type definitions, something like `%type` in Yacc.
- Consider allowing additional arguments and keeping all but the last argument in scanner methods.
- Consider adding `precedenceGroup` elements to XML.
- Consider using a class or struct instead of object in the Debug configuration.

## Both

I plan to produce only errors, no warnings, converting current warnings into errors.

## Build

The solution requires a specific build order.

1. Build the `Lad` project until it doesn't fail.  This will be no more than three times.
1. Build the `Pard` project until it doesn't fail.  This will be no more than two times.
1. Build the solution until it doesn't fail.  This will be no more than two times.  This will build the unit test projects.

Instead of building the projects and the solution, build the `Make` project.  The solution is configured to skip it so you must
build it explicitly.  It will build all other projects.
