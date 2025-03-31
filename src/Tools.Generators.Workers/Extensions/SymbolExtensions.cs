using Microsoft.CodeAnalysis;

namespace Tools.Generators.Workers.Extensions;

public static class SymbolExtensions
{
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
        return symbol.AllInterfaces.Any(@interface => @interface.IsOfType(baseType));
    }

    public static bool IsPublicOrInternalClass(this INamedTypeSymbol symbol)
    {
        var accessibility = symbol.DeclaredAccessibility;
        return accessibility is Accessibility.Public or Accessibility.Internal;
    }

    private static bool IsOfType(this ISymbol symbol, INamedTypeSymbol baseType)
    {
        return SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, baseType);
    }
}