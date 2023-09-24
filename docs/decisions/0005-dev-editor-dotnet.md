# Editor Tool (.NET)

* status: accepted
* date: 2023-09-08
* deciders: jezzsantos

# Context and Problem Statement

This codebase is a codebase "template" that is expected to be used by all developers.

Since it contains source code, unfortunately, some of those files will have to be designed specifically for the tools, being used, to manipulate it.

Ideally, all dev tools (from all vendors) would use the same tooling and standards, and use the same configuration files, etc. But that is not the case, even between the 3 most common dev tools for writing .NET code: Jetbrains Rider, Visual Studio and VS Code. Each tool has its own ways of doing things.

Since these differences exist (in the non-ideal, real-world), we will have to make decisions on what toolset to best support, and do our very best not to marginalize people using other dev tools, especially since they may not have a choice about that.

Many of these editing tools are compatible with many files in a single codebase, using standard formats, etc. Some are not.

## Considered Options

The most popular .NET solution, project and file editors for C#, in the world are:

1. Jetbrains Rider
2. Visual Studio Code
3. Visual Studio 20XX

## Decision Outcome

`Jetbrains Rider`

* This tool defines more advanced code editing settings (i.e. inclusion of Resharper).
* This tool has gained more popularity in the last few years for pure C# development, whereas Visual Studio Code has gained more popular for Javascript development.
* The template is predominantly C# development, but may also include some TypeScript development.