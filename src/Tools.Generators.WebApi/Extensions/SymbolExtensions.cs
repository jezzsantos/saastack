using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tools.Generators.WebApi.Extensions;

public static class SymbolExtensions
{
    public static AttributeData? GetAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
    {
        return symbol.GetAttributes()
            .FirstOrDefault(attribute => attribute.AttributeClass!.IsOfType(attributeType));
    }

    public static INamedTypeSymbol? GetBaseType(this ITypeSymbol symbol, INamedTypeSymbol baseType)
    {
        return symbol.AllInterfaces.FirstOrDefault(@interface => @interface.IsOfType(baseType));
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

    public static IEnumerable<string> GetUsingNamespaces(this INamedTypeSymbol symbol)
    {
        var syntaxReference = symbol.DeclaringSyntaxReferences.IsDefaultOrEmpty
            ? null
            : symbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference is null)
        {
            return Enumerable.Empty<string>();
        }

        var usingSyntaxes = syntaxReference.SyntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>();

        return usingSyntaxes.Select(us => us.Name!.ToString())
            .Distinct()
            .OrderDescending()
            .ToList();
    }

    public static bool IsClass(this ITypeSymbol type)
    {
        return type.TypeKind == TypeKind.Class;
    }

    public static bool IsConcreteInstanceClass(this INamedTypeSymbol symbol)
    {
        return symbol is { IsAbstract: false, IsStatic: false };
    }

    public static bool IsDerivedFrom(this ITypeSymbol symbol, INamedTypeSymbol baseType)
    {
        ArgumentNullException.ThrowIfNull(baseType);
        return symbol.AllInterfaces.Any(@interface => @interface.IsOfType(baseType));
    }

    public static bool IsOfType(this ISymbol symbol, INamedTypeSymbol baseType)
    {
        return SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, baseType);
    }

    public static bool IsParameterless(this IMethodSymbol symbol)
    {
        return symbol.Parameters.Length == 0;
    }

    public static bool IsPublicInstance(this IMethodSymbol symbol)
    {
        return symbol is { IsStatic: false, DeclaredAccessibility: Accessibility.Public };
    }

    public static bool IsPublicOrInternalClass(this INamedTypeSymbol symbol)
    {
        var accessibility = symbol.DeclaredAccessibility;
        return accessibility is Accessibility.Public or Accessibility.Internal;
    }

    public static bool IsPublicOrInternalInstanceMethod(this IMethodSymbol symbol)
    {
        return symbol is { IsStatic: false, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal };
    }
}