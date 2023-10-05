extern alias Analyzers;
using System.Reflection;
using Analyzers::Infrastructure.WebApi.Common;
using Common.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using NuGet.Frameworks;

namespace Tools.Analyzers.Core.UnitTests;

public static class Verify
{
    private static readonly Assembly[] AdditionalReferences =
    {
        typeof(Verify).Assembly,
        //typeof(Error).Assembly,
        typeof(ApiEmptyResult).Assembly
        //typeof(IWebApiService).Assembly
    };

    // HACK: we have to define the .NET 7.0 framework here,
    // because the current version of Microsoft.CodeAnalysis.Testing.ReferenceAssemblies
    // does not contain a value for this framework  
    private static readonly Lazy<ReferenceAssemblies> LazyNet70 = new(() =>
    {
        if (!NuGetFramework.Parse("net7.0")
                .IsPackageBased)
        {
            // The NuGet version provided at runtime does not recognize the 'net6.0' target framework
            throw new NotSupportedException("The 'net6.0' target framework is not supported by this version of NuGet.");
        }

        return new ReferenceAssemblies("net7.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "7.0.0"),
            Path.Combine("ref", "net7.0"));
    });

    private static ReferenceAssemblies Net70 => LazyNet70.Value;

    public static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        int locationX, int locationY, string argument, params object?[]? messageArgs)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await DiagnosticExists<TAnalyzer>(descriptor, inputSnippet, (locationX, locationY, argument), messageArgs);
    }

    public static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        (int, int, string) expected1, (int, int, string) expected2)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var expectation1 = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor)
            .WithLocation(expected1.Item1, expected1.Item2)
            .WithArguments(expected1.Item3);
        var expectation2 = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor)
            .WithLocation(expected2.Item1, expected2.Item2)
            .WithArguments(expected2.Item3);

        await RunAnalyzerTest<TAnalyzer>(inputSnippet, new[] { expectation1, expectation2 });
    }

    public static async Task NoDiagnosticExists<TAnalyzer>(string inputSnippet)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await RunAnalyzerTest<TAnalyzer>(inputSnippet, null);
    }

    private static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        (int, int, string) expected1, params object?[]? messageArgs)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var arguments = messageArgs.Exists() && messageArgs!.Any()
            ? new object[] { expected1.Item3 }.Concat(messageArgs!)
            : new object[] { expected1.Item3 };

        var expectation = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor)
            .WithLocation(expected1.Item1, expected1.Item2)
            .WithArguments(arguments.ToArray()!);

        await RunAnalyzerTest<TAnalyzer>(inputSnippet, new[] { expectation });
    }

    private static async Task RunAnalyzerTest<TAnalyzer>(string inputSnippet, DiagnosticResult[]? expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var analyzerTest = new CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>();
        foreach (var assembly in AdditionalReferences)
        {
            analyzerTest.TestState.AdditionalReferences.Add(assembly);
        }

        analyzerTest.ReferenceAssemblies = Net70;
        analyzerTest.TestCode = inputSnippet;
        if (expected is not null && expected.Any())
        {
            analyzerTest.ExpectedDiagnostics.AddRange(expected);
        }

        await analyzerTest.RunAsync(CancellationToken.None);
    }
}