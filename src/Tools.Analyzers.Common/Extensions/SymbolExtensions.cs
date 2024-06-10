using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Common.Extensions;

public static class SymbolExtensions
{
    public static bool HasParameterlessConstructor(this INamedTypeSymbol symbol)
    {
        var constructors = symbol.InstanceConstructors;
        if (constructors.Length == 0)
        {
            return true;
        }

        return symbol.InstanceConstructors
            .Any(c => c.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public
                      && c.Parameters.Length == 0);
    }

    public static bool HasPropertiesOfAllowableTypes(this INamedTypeSymbol symbol,
        List<INamedTypeSymbol> allowableTypes)
    {
        var properties = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(p => p.GetMethod?.ReturnType)
            .Where(rt => rt is not null);

        foreach (var property in properties)
        {
            if (!allowableTypes.Any(allowableType => property!.IsOfType(allowableType)))
            {
                return false;
            }
        }

        return true;
    }

    public static bool HasPublicGetterAndSetterProperties(this INamedTypeSymbol symbol)
    {
        var properties = symbol.GetMembers()
            .OfType<IPropertySymbol>();

        foreach (var property in properties)
        {
            if (property.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Public)
            {
                return false;
            }

            if (property.GetMethod is null || property.SetMethod is null)
            {
                return false;
            }
        }

        return true;
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

    public static AttributeData? GetAttributeOfType<TAttribute>(this ISymbol? symbol,
        SyntaxNodeAnalysisContext context)
    {
        return GetAttributeOfType<TAttribute>(symbol, context.Compilation);
    }

    public static AttributeData? GetAttributeOfType<TAttribute>(this ISymbol? symbol,
        Compilation compilation)
    {
        if (symbol is null)
        {
            return null;
        }

        var attributeMetadata = compilation.GetTypeByMetadataName(typeof(TAttribute).FullName!)!;
        var attributes = symbol.GetAttributes();

        return attributes.FirstOrDefault(attr => attr.AttributeClass!.IsOfType(attributeMetadata));
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