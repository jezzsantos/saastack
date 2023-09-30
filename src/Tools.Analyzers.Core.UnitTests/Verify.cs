using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace Tools.Analyzers.Core.UnitTests;

public static class Verify
{
    public static async Task DiagnosticExists(string diagnosticId, string inputSnippet, int locationX, int locationY,
        string argument = "AClass")
    {
        var expected = CSharpAnalyzerVerifier<MissingDocsAnalyzer, DefaultVerifier>.Diagnostic(diagnosticId)
            .WithLocation(locationX, locationY)
            .WithArguments(argument);
        await CSharpAnalyzerVerifier<MissingDocsAnalyzer, DefaultVerifier>.VerifyAnalyzerAsync(inputSnippet, expected);
    }

    public static async Task NoDiagnosticExists(string inputSnippet)
    {
        await CSharpAnalyzerVerifier<MissingDocsAnalyzer, DefaultVerifier>.VerifyAnalyzerAsync(inputSnippet);
    }
}