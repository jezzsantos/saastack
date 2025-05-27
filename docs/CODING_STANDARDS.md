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

## Breaking changes

General overview of breaking versus non-breaking



The things that have been explicitly designed to change easily, and avoid creating breaking changes





Things you CANNOT change without significant and dire consequences to already deployed and running Production systems:

1. Events
2. ReadModels (in SQL)
3. Caution with existing, Events on the queues, and message bus

## Tooling to use

These are the tooling that you should use frequently