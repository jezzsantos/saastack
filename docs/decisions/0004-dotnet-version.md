# DotNet version

* status: accepted
* date: 2024-03-11
* deciders: jezzsantos

# Context and Problem Statement

We need to choose a .NET version to support this codebase.

It should be consistent across all components in the codebase.

It should align with [Microsoft's support strategy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), ideally, it would be an LTS version.

It should provide the functionality we are basing the patterns in the codebase on

It should NOT be a re-release version, it should be a stable version(a.k.a not on the bleeding edge), and well supported in the .NET eco-system of 3rd party libraries and frameworks.

## Considered Options

The options are:

1. .NET 8
2. .NET 7
3. .NET 6

## Decision Outcome

`.NET 8.0`

- .NET 6- even though it is the current LTS, does not support the minimal API features (e.g. the `[AsParamtersAttribute]`) that we need to easily build the desired [Web API Framework](0050-api-framework.md) we want to have.
- .NET 7 - is the current STS version, but it is not an LTS version, it does not support named dependencies in the DI.