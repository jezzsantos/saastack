extern alias CommonAnalyzers;
extern alias NonFrameworkAnalyzers;
using System.Reflection;
using Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using NonFrameworkAnalyzers::Common.Extensions;
using NonFrameworkAnalyzers::QueryAny;
using NuGet.Frameworks;
using AnalyzerConstants = CommonAnalyzers::Tools.Analyzers.Common.AnalyzerConstants;
using Task = System.Threading.Tasks.Task;

namespace Tools.Analyzers.NonFramework.UnitTests;

public static class Verify
{
    /// <summary>
    ///     Provides references to code that we are using in the testing code snippets
    /// </summary>
    private static readonly Assembly[] AdditionalReferences =
    {
        typeof(Verify).Assembly,
        typeof(AnalyzerConstants).Assembly,
        typeof(IQueryableEntity).Assembly
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

    public static async Task CodeFixed<TAnalyzer, TCodeFix>(DiagnosticDescriptor descriptor, string problem, string fix,
        int locationX, int locationY, params object[] arguments)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        await CodeFixed<TAnalyzer, TCodeFix>(descriptor, null, problem, fix, locationX, locationY, arguments);
    }

    public static async Task CodeFixed<TAnalyzer, TCodeFix>(DiagnosticDescriptor descriptor, string problem, string fix,
        int locationX, int locationY, object[] arguments,
        params (DiagnosticDescriptor descriptor, int locationX, int locationY, string argument, object?[]? messageArgs)
            [] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var expectations = new[]
            {
                CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor)
                    .WithLocation(locationX, locationY)
                    .WithArguments(arguments)
            }.Concat(expected
                .Select(exp =>
                {
                    var args = exp.messageArgs.Exists() && exp.messageArgs.Length != 0
                        ? new object[] { exp.argument }.Concat(exp.messageArgs)
                        : new object[] { exp.argument };
                    return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>
                        .Diagnostic(exp.descriptor)
                        .WithLocation(exp.locationX, exp.locationY)
                        .WithArguments(args.ToArray()!);
                }))
            .ToArray();

        await RunCodeFixTest<TAnalyzer, TCodeFix>(problem, fix, null, expectations);
    }

    public static async Task CodeFixed<TAnalyzer, TCodeFix>(DiagnosticDescriptor descriptor, string? equivalenceKey,
        string problem, string fix, int locationX, int locationY, params object[] arguments)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var expectation = CSharpCodeFixVerifier<TAnalyzer, TCodeFix, DefaultVerifier>.Diagnostic(descriptor)
            .WithLocation(locationX, locationY)
            .WithArguments(arguments);

        await RunCodeFixTest<TAnalyzer, TCodeFix>(problem, fix, equivalenceKey, new[] { expectation });
    }

    public static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        int locationX, int locationY, string argument, params object?[]? messageArgs)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        await DiagnosticExists<TAnalyzer>(descriptor, inputSnippet, (locationX, locationY, argument), messageArgs);
    }

    public static async Task DiagnosticExists<TAnalyzer>(DiagnosticDescriptor descriptor, string inputSnippet,
        params (int locationX, int locationY, string argument)[] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var expectations = expected
            .Select(exp => CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>
                .Diagnostic(descriptor)
                .WithLocation(exp.locationX, exp.locationY)
                .WithArguments(exp.argument))
            .ToArray();

        await RunAnalyzerTest<TAnalyzer>(inputSnippet, expectations);
    }

    public static async Task DiagnosticExists<TAnalyzer>(string inputSnippet,
        params (DiagnosticDescriptor descriptor, int locationX, int locationY, string argument, object?[]? messageArgs)
            [] expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        var expectations = expected
            .Select(exp =>
            {
                var args = exp.messageArgs.Exists() && exp.messageArgs.Length != 0
                    ? new object[] { exp.argument }.Concat(exp.messageArgs)
                    : new object[] { exp.argument };
                return CSharpAnalyzerVerifier<TAnalyzer, DefaultVerifier>
                    .Diagnostic(exp.descriptor)
                    .WithLocation(exp.locationX, exp.locationY)
                    .WithArguments(args.ToArray()!);
            })
            .ToArray();

        await RunAnalyzerTest<TAnalyzer>(inputSnippet, expectations);
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

    private static async Task RunCodeFixTest<TAnalyzer, TCodeFix>(string problem, string fix, string? equivalenceKey,
        DiagnosticResult[]? expected)
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        var codeFixTest = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>();
        foreach (var assembly in AdditionalReferences)
        {
            codeFixTest.TestState.AdditionalReferences.Add(assembly);
        }

        codeFixTest.ReferenceAssemblies = Net80;
        codeFixTest.TestCode = problem;
        codeFixTest.FixedCode = fix;

        if (expected is not null && expected.Any())
        {
            codeFixTest.ExpectedDiagnostics.AddRange(expected);
        }

        codeFixTest.CodeActionEquivalenceKey = equivalenceKey;

        await codeFixTest.RunAsync(CancellationToken.None);
    }
}