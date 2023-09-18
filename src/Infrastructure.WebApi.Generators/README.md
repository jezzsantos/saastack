# Source Generator

This source generator project is meant to be included by every Api Project for every subdomain.

It's job is to convert any `IWebApiService` class definitions (found in the assembly) into Minimal API registrations and MediatR handlers.

# Development Workarounds

C# Source Generators have difficulties running in the IDE if the code used in them has dependencies on other projects in the solution (and other nugets).

This is especially problematic when those referenced projects have transient dependencies to types in AspNetCore.

If any dependencies are taken, special workarounds (in the project file of this project) are required in order for this source generators to work properly.

We are avoiding including certain types from any projects in this solution (e.g. from the `Infrastructure.WebApi.Interfaces` project) even though we need in the code of the Source generator, since that project is dependent on types in AspNetCore framework.

To workaround this, we have file-linked certain source files from projects in the solution, so that we can use those symbols in the Source Generator code.

We have had to hardcode certain other types to avoid referencing AspNetCore, and these cannot be tracked by tooling if they are changed elsewhere.

> None of this is ideal. But until we can figure the magic needed to build and run this Source Generator if it uses these types, this may be the best workaround we have for now.