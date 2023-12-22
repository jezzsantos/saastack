extern alias CommonAnalyzers;
using System.Reflection;
using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using NuGet.Frameworks;
using QueryAny;
using AnalyzerConstants = CommonAnalyzers::Tools.Analyzers.Common.AnalyzerConstants;
using Task = System.Threading.Tasks.Task;

namespace Tools.Analyzers.Subdomain.UnitTests;

public static class Verify
{
    /// <summary>
    ///     Provides references to code that we are using in the testing code snippets
    /// </summary>
    private static readonly Assembly[] AdditionalReferences =
    {
        typeof(Verify).Assembly,
        typeof(AnalyzerConstants).Assembly,
        typeof(Query).Assembly,
        typeof(CommonMarker).Assembly,
        typeof(DomainInterfacesMarker).Assembly,
        typeof(DomainCommonMarker).Assembly,
        typeof(InfrastructureWebApiInterfacesMarker).Assembly,
        typeof(InfrastructureWebApiCommonMarker).Assembly
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

        return new ReferenceAssemblies("net7.0", new PackageIdentity("Microsoft.NETCore.App.Ref", "7.0.14"),
            Path.Combine("ref", "net7.0"));
    });

    private static ReferenceAssemblies Net70 => LazyNet70.Value;

    public static async Task CodeFixed<TAnalyzer, TCodeFix>(DiagnosticDescriptor descriptor, string problem, string fix,
        int locationX, int locationY, string argument)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var codeFixTest = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>();
        foreach (var assembly in AdditionalReferences)
        {
            codeFixTest.TestState.AdditionalReferences.Add(assembly);
        }

        codeFixTest.ReferenceAssemblies = Net70;
        codeFixTest.TestCode = problem;
        codeFixTest.FixedCode = fix;

        var expected = CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor)
            .WithLocation(locationX, locationY)
            .WithArguments(argument);
        codeFixTest.ExpectedDiagnostics.Add(expected);

        await codeFixTest.RunAsync(CancellationToken.None);
    }

    public static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        int locationX, int locationY, string argument, params object?[]? messageArgs)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await DiagnosticExists<TAnalyzer>(descriptor, inputSnippet, (locationX, locationY, argument), messageArgs);
    }

    public static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        (int locationX, int locationY, string argument) expected1,
        (int locationX, int locationY, string argument) expected2)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var expectation1 = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor)
            .WithLocation(expected1.locationX, expected1.locationY)
            .WithArguments(expected1.argument);
        var expectation2 = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor)
            .WithLocation(expected2.locationX, expected2.locationY)
            .WithArguments(expected2.argument);

        await RunAnalyzerTest<TAnalyzer>(inputSnippet, new[] { expectation1, expectation2 });
    }

    public static async Task DiagnosticExists<TAnalyzer>(string inputSnippet,
        (DiagnosticDescriptor descriptor, int locationX, int locationY, string argument) expected1,
        (DiagnosticDescriptor descriptor, int locationX, int locationY, string argument) expected2)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var expectation1 = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(expected1.descriptor)
            .WithLocation(expected1.locationX, expected1.locationY)
            .WithArguments(expected1.argument);
        var expectation2 = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(expected2.descriptor)
            .WithLocation(expected2.locationX, expected2.locationY)
            .WithArguments(expected2.argument);

        await RunAnalyzerTest<TAnalyzer>(inputSnippet, new[] { expectation1, expectation2 });
    }

    public static async Task NoDiagnosticExists<TAnalyzer>(string inputSnippet)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await RunAnalyzerTest<TAnalyzer>(inputSnippet, null);
    }

    private static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        (int locationX, int locationY, string argument) expected1, params object?[]? messageArgs)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var arguments = messageArgs.Exists() && messageArgs.Any()
            ? new object[] { expected1.argument }.Concat(messageArgs)
            : new object[] { expected1.argument };

        var expectation = CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>.Diagnostic(descriptor)
            .WithArguments(arguments.ToArray()!)
            .WithLocation(expected1.locationX, expected1.locationY);

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