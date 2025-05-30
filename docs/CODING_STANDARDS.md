# Coding Standards

The team contributing to this product wish to standardise certain practices, styles, principles and approaches. The document aims to capture most of them.

> At anytime, the team is open to revise anything in this document, but because consistency is the key driver behind this document, when things change, they change right across this whole repo. The last outcome anyone wants is inconsistencies that are hard to understand and draw bounds around.

## Patterns and conventions

Why are they important.

How can I list or expose all the patterns? Perhaps just the principles behind them.

## Code replacements

C# coding standards

Rules we have defined in Rider

Code that we don't want to see, and instead replaced with other code.

Discussion about Optional versus Nullable



Common dotnet expressions, that we want replaced in the code:

| Instead of this:                   | Use this:                             | Why?                                                         |
| ---------------------------------- | ------------------------------------- | ------------------------------------------------------------ |
| `DateTime.Now`                     | `DateTime.UtcNow`                     | You should never handle local dates and times in the API layer. All dates and times should always be in UTC. Only clients should convert to dates and times to local, based on client settings. |
| `!string.IsNullOrEmpty(variable)`  | `variable.HasValue()`                 | Easier to read and understand the real intent                |
| `string.IsNullOrEmpty(variable)`   | `variable.HasNoValue()`               | Easier to read and understand the real intent                |
| `variable != null`                 | `variable.Exists()`                   | Easier to understand the real intent                         |
| `variable == null`                 | `variable.NotExists()`                | Easier to understand the real intent                         |
| `variable == null`                 | `variable.IsNull()`                   | Uncommon, for completeness in these rare cases.              |
| `variable != null`                 | `variable.IsNotNull`                  | Uncommon, for completeness in these rare cases.              |
| `string.Format(message, args)`     | `message.Format(args)`                |                                                              |
| `variable.Equals(value, options)`  | `variable.EqualsIgnoreCase(value)`    |                                                              |
| `!variable.Equals(value, options)` | `variable.NotEqualsIgnoreCase(value)` |                                                              |
| `collection.Any()`                 | `collection.HasAny()`                 |                                                              |
| `!collection.Any()`                | `collection.HasNone()`                |                                                              |





## Breaking changes

General overview of breaking versus non-breaking



The things that have been explicitly designed to change easily, and avoid creating breaking changes





Things you CANNOT change without significant and dire consequences to already deployed and running Production systems:

1. Events
2. ReadModels (in SQL)
3. Caution with existing, Events on the queues, and message bus

## Tooling to use

These are the tooling that you should use frequently