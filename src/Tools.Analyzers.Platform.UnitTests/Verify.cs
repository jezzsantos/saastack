extern alias CommonAnalyzers;
extern alias PlatformAnalyzers;
using System.Reflection;
using Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using PlatformAnalyzers::Common.Extensions;
using NuGet.Frameworks;
using AnalyzerConstants = CommonAnalyzers::Tools.Analyzers.Common.AnalyzerConstants;
using Task = System.Threading.Tasks.Task;

namespace Tools.Analyzers.Platform.UnitTests;

public static class Verify
{
    /// <summary>
    ///     Provides references to code that we are using in the testing code snippets
    /// </summary>
    private static readonly Assembly[] AdditionalReferences =
    {
        typeof(Verify).Assembly,
        typeof(AnalyzerConstants).Assembly
    };

    // HACK: we have to define the .NET 8.0 framework here,
    // because the current version of Microsoft.CodeAnalysis.Testing.ReferenceAssemblies
    // does not contain a value for this framework  
    private static readonly Lazy<ReferenceAssemblies> LazyNet80 = new(() =>
    {
        const string version = $"net{RuntimeConstants.Dotnet.Version}";
        const string sdkVersion = RuntimeConstants.Dotnet.RuntimeVersion;
        if (!NuGetFramework.Parse(version)
                .IsPackageBased)
        {
            // The NuGet version provided at runtime does not recognize the target framework
            throw new NotSupportedException(
                $"The '{version}' target framework is not supported by this version of NuGet.");
        }

        return new ReferenceAssemblies(version, new PackageIdentity("Microsoft.NETCore.App.Ref", sdkVersion),
            Path.Combine("ref", version));
    });

    private static ReferenceAssemblies Net80 => LazyNet80.Value;

    public static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        int locationX, int locationY, string argument, params object?[]? messageArgs)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await DiagnosticExists<TAnalyzer>(descriptor, inputSnippet, (locationX, locationY, argument), messageArgs);
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

        analyzerTest.ReferenceAssemblies = Net80;
        analyzerTest.TestCode = inputSnippet;
        if (expected is not null && expected.Any())
        {
            analyzerTest.ExpectedDiagnostics.AddRange(expected);
        }

        await analyzerTest.RunAsync(CancellationToken.None);
    }
}