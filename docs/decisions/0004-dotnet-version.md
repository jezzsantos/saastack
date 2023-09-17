# DotNet version

* status: accepted
* date: 2023-09-17
* deciders: jezzsantos

# Context and Problem Statement

We need to choose a .NET version to support this codebase.

It should be consistent across all components in the codebase.

It should align with [Microsoft's support strategy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), ideally, it would be an LTS version.

It should provide the functionality we are basing the patterns in the codebase on

It should NOT be a re-release version, it should be a stable version(a.k.a not on the bleeding edge), and well supported in the .NET eco-system of 3rd party libraries and frameworks.

## Considered Options

The options are:

1. .NET 7
2. .NET 6
3. .NET 8

## Decision Outcome

`.NET 7.0`

- .NET 6- even though it is the current LTS, does not support the minimal API features (e.g. the `[AsParamtersAttribute]`) that we need to easily build the desired [Web API Framework](0050-api-framework.md) we want to have.
- .NET 8 - is the next LTS version, but has not released as of yet, but when it is released (Nov '23) the adjacent eco-system will need some time to catch up. Until .NET 8 has been well-adopted, we risk the chance that some organizations will not permit starting their products on STS releases. However, anytime after Nov '23, the codebase should be easily upgradable to .NET 8.
