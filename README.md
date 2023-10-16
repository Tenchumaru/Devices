# Devices

This is a solution containing projects that are .NET implementations of lex/flex and yacc/bison, the compiler tools.

## Lad

The scanner requires a field, `reader`, of type `TextReader`.

C# and Lad treat `"a\"b"` and `@"a""b"` the same.
C# and Lad treat `"\a\"\b"` and `@"\a""\b"` differently since Lad always interprets escape sequences.
	C# sees three and five characters, respectively, while Lad sees three characters in both.
Specifying an initial `$` to signal a literal string results in each of those four strings having its own interpretation.

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
