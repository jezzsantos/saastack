# Source Generator

This source generator project is meant to be included by every Api Project for every subdomain.

It's job is to convert `IWebApiService` class definitions into Minimal API definitions using MediatR handlers.

# Development Workarounds

C# Source Generators have difficulties running in the IDE if the code used in them has dependencies on other projects in the solution (and other nugets).
Th is is especially problematic when those referenced projects have transient dependencies to other frameworks.

Special workarounds (in the project file of this project) are required in order for this source generators to work.

We are avoiding including certain types from the `Infrastructure.WebApi.Interfaces` project (even though we need them), since that project is dependent on types in:

* MediatR.Contracts
* The AspNetCore framework

We have file linked certain source files from that project, so that we can use those symbols in the Source Generator code, and track those names should they change in the future
We have had to hardcode certain other types to avoid referencing AspNetCore and MediatR assemblies, and these cannot be tracked if changed elsewhere.

None of this is ideal. But until we can figure the magic needed to build and run this Source Generator if it uses these types, this may be the best workaround we have for now. 