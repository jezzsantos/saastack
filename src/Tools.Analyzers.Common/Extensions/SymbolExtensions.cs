using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Common.Extensions;

public static class SymbolExtensions
{
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

    public static bool IsEnum(this ITypeSymbol symbol)
    {
        return symbol.TypeKind == TypeKind.Enum;
    }

    public static bool IsNullable(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        return symbol.NullableAnnotation == NullableAnnotation.Annotated;
    }

    public static bool IsOfType(this ISymbol symbol, INamedTypeSymbol baseType)
    {
        return SymbolEqualityComparer.Default.Equals(symbol, baseType);
    }

    public static bool IsVoid(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        return IsVoid(symbol, context.Compilation);
    }

    public static bool IsVoid(this ITypeSymbol symbol, Compilation compilation)
    {
        var voidSymbol = compilation.GetTypeByMetadataName(typeof(void).FullName!)!;
        return IsOfType(symbol, voidSymbol);
    }

    public static bool IsVoidTask(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var taskType = context.Compilation.GetTypeByMetadataName(typeof(Task).FullName!)!;
        return IsOfType(symbol, taskType);
    }

    public static ITypeSymbol WithoutNullable(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        if (symbol.IsNullable(context))
        {
            if (symbol.IsReferenceType)
            {
                if (((INamedTypeSymbol)symbol).IsGenericType) // e.g. List<T> or Dictionary<TKy, TValue>
                {
                    return symbol;
                }

                return symbol.OriginalDefinition;
            }

            return ((INamedTypeSymbol)symbol).TypeArguments[0]; // e.g. a ValueType like DataTime or an Enum
        }

        return symbol;
    }
}