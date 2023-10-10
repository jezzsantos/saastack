using Microsoft.CodeAnalysis;

namespace Tools.Analyzers.Core.Extensions;

public static class SymbolExtensions
{
    public static bool IsOfType(this ISymbol symbol, INamedTypeSymbol baseType)
    {
        return SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, baseType);
    }
}