# Analyzers

This analyzer project is meant to be included by every project in the solution. It contains several analyzers.

The individual analyzers will filter the individual projects their analyzers apply to.

# Development Workarounds

C# Analyzers have difficulties running in the IDE if the code used in them has dependencies on other projects in the solution (and other nugets).

This is especially problematic when those referenced projects have transient dependencies to types in .Net or AspNet.

If any dependencies are taken, special workarounds (in the project file of this project) are required in order for the analyzers to work properly.

We are avoiding including certain types from any projects in this solution (e.g. from the `Infrastructure.WebApi.Common` and `Infrastructure.Web.Api.Interfaces` project) even though we need it in the code of the Analyzers, since that project is dependent on types in AspNet framework.

To workaround this, we have file-linked certain source files from projects in the solution, so that we can use those symbols in the Analyzer code.

We have had to hardcode certain other types to avoid referencing AspNet, and these cannot be tracked by tooling if they are changed elsewhere.

> None of this is ideal. But until we can figure the magic needed to build and run these Analyzers if it uses these types, this may be the best workaround we have for now.

# Debugging Analyzers

You can debug the analyzers easily from the unit tests.

You can debug your analyzers by setting a breakpoint in the code, and then running the `Tools-Analyzers-Platform-Development` run configuration from the `Tools.Analyzers.Platform` project with the debugger. (found in the `launchSettings.json` file in any executable project).

# Updating Analyzers

After you have made changes to the analyzers they need to be refreshed in the current solution, as most of the projects in the solution are already using the Analyzers.

1. Build and pack this project: `dotnet pack` (this will copy the nuget to the `..\..\tools\nuget` directory)
2. Clear all caches for the local source (including the one at: `..\..\tools\nuget`): `dotnet nuget locals all --clear`
3. restore all packages for the solution: `dotnet restore`
