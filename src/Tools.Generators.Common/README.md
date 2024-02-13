# Source Generator

This source generator project is only meant to be included by the `Common` project only.

It's job is to convert all `FeatureFlags` definition (found in the assembly) into instances of the `Flag` class.

# Development Workarounds

Source Generators are required to run to build the rest of the codebase.

Source Generators have to be built in NETSTANDARD2.0 for them to run in Visual Studio, but this is not the case to run in JetBrains Rider.
> This constraint exists to support source generators working in older versions of the .NET Framework, and will exist until Microsoft fix the issue Visual Studio. This is another reason to use JetBrains Rider as the preferred IDE for working with this codebase.

C# Source Generators have difficulties running in any IDE if the code used in them references code in other projects in the solution, and they also suffer problems if they reference any nuget packages.

This is especially problematic when those referenced projects have transient dependencies to types in ASP.NET

If any dependencies are taken, special workarounds (in the project file of this project) are required in order for this source generators to work properly.

We are avoiding including certain types from any projects in this solution (e.g. from the `Common` project) even though we need it in the code of the Source generator, since that project is dependent on types in AspNet framework.

To workaround this, we have file-linked certain source files from projects in the solution, so that we can use those symbols in the Source Generator code.

We have had to hardcode certain other types to avoid referencing AspNet, and these cannot be tracked by tooling if they are changed elsewhere.

> None of this is ideal. But until we can figure the magic needed to build and run this Source Generator if it uses these types, this may be the best workaround we have for now.

# Debugging Generators

You can debug the analyzers easily from the unit tests.

You can debug your source generator by setting a breakpoint in the code, and then running the `Common-SourceGenerators-Development` run configuration from the `ApiHost1` project with the debugger. (found in the `launchSettings.json` file in any executable project).


> Warning: C# source generators are heavily cached. If you try to debug new code that you've added you may need to clear the caches from the old code being used. Otherwise you breakpoints may not hit.

The most reliable way to reset the generators:

1. Restart Jetbrains Rider
2. Kill any remaining `.Net Host (dotnet.exe)` processes on your machine, and any remaining `Jetbrains Rider` processes on your machine
3. Restart Rider
4. Set your breakpoints
5. Start debugging the `Common-SourceGenerators-Development` run configuration