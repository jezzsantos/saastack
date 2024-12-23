# Nullability

* status: accepted
* date: 2023-09-12
* deciders: jezzsantos

# Context and Problem Statement

Nullability refers to the issue of managing nulls in code.
Managing null in C# has been a long-standing issue, and the language has made several advances to reduce the amount of handling required.
However, the issue still remains, and the language has not yet provided a definitive solution. Thus, many developers are still guarding against nulls and handling them as results.

Nullable value types were first introduced in C# 2.0 (circa, Nov 2005), which allowed "value types" (like `int?`, `DateTime?`, and `MyCustomEnum?` etc.) to be explicitly assigned a null value.
Since C# 8 (circa, Sept 2019), we've had the ability to define nullable "reference types" (like `string?` or `MyCustomObject?`), which allow developers to annotate their code to indicate where nulls are allowed. We also had the ability to mark up code, and projects with nullable annotations to help the compiler identify potential null reference exceptions. (like `#nullable enable` or `#nullable disable`) and make whole projects nullable or not using `<Nullable>enable</Nullable>`. Which also added tooling like Roslyn rules to help identify potential null reference exceptions.

Outside of official dotnet releases, we've seen the introduction of more functional programming concepts like `Option<T>`, `OneOf<T>` and `Result<T>` types, which are used to wrap values that may or may not be null, and provide a more functional way of handling nulls.

These help the programmer manage return types much easier (since they are often implemented as `struct`) and that can lead to the eradication of `null` as a value anywhere in the code (except dealing with 3rd party library code).

## Considered Options

The options are:

1. Nullable Context - having the compiler and IDE help identify potential null reference exceptions.
2. Optional<T> - representing a value that may or may not be present.
3. Nulls - same old, same old.

## Decision Outcome

`Nullable Context + Optional<T>`

- Neither just Nullable Context nor Optional<T> alone is sufficient to completely eradicate nulls form the codebase. Both are useful in certain contexts. It largely depends on what is being expressed at the time.
- Nullable Context (in an IDE) is very useful to help the programmer recognize where nulls are allowed, and where they are not. It also helps the compiler to identify potential null reference exceptions.
- Optional<T> is very useful when dealing with return types, and when you want to express that a value may or may not be present. It is also useful when you want to express that a value may or may not be present, and you want to provide a default value if it is not present.
- {justification3}

## More Information

The dotnet team has attempted to introduce a set of functional types including `Optional<T>` in the past in the [dotNext](https://github.com/dotnet/dotNext) which many products/teams have adopted. However, there have been strong statements from the dotnet team (can't find it online) that they will NOT be adopting these types into the dotnet runtime (some statement like: "we don't want another `null`!").

However, in the interim, we have moved forward with our own slim version of `Optional<T>` and `Result<T>`types, which are based on the [dotNext](https://github.com/dotnet/dotNext) project:

- We decided to reduce the number of dependencies in this codebase
- We decided to learn more about these types, and how they could be used in our codebase.
- We decided that one day they can be (relatively easily) ripped and replaced for another implementation (at some cost), either when the language official supports them, or when we want to commit to another library.
