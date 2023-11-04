using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Platform.Extensions;

public static class SymbolExtensions
{
    public static bool IsOfType(this ISymbol symbol, INamedTypeSymbol baseType)
    {
        return SymbolEqualityComparer.Default.Equals(symbol, baseType);
    }

    public static bool IsVoid(this ITypeSymbol returnType, SyntaxNodeAnalysisContext context)
    {
        var voidSymbol = context.Compilation.GetTypeByMetadataName(typeof(void).FullName!)!;
        return returnType.IsOfType(voidSymbol);
    }

    public static bool IsVoidTask(this ITypeSymbol returnType, SyntaxNodeAnalysisContext context)
    {
        var taskSymbol = context.Compilation.GetTypeByMetadataName(typeof(Task).FullName!)!;
        return returnType.IsOfType(taskSymbol);
    }

    public static string GetMethodBody(this ISymbol method)
    {
        var syntaxReference = method.DeclaringSyntaxReferences.FirstOrDefault();

        var syntax = syntaxReference?.GetSyntax();
        if (syntax is MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.Body?.ToFullString() ?? string.Empty;
        }

        return string.Empty;
    }
}