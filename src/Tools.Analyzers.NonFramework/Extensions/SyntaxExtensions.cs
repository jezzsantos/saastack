using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using QueryAny;
using Tools.Analyzers.Common.Extensions;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;

namespace Tools.Analyzers.NonFramework.Extensions;

public static class SyntaxExtensions
{
    public static bool HasEntityNameAttribute(this ClassDeclarationSyntax classDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        return HasEntityNameAttribute(context.SemanticModel, context.Compilation, classDeclarationSyntax);
    }

    public static bool HasEntityNameAttribute(this SemanticModel semanticModel, Compilation compilation,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var attribute = classDeclarationSyntax.GetAttributeOfType<EntityNameAttribute>(semanticModel, compilation);
        return attribute.Exists();
    }

    public static bool HasOnlyPrivateInstanceConstructors(this ClassDeclarationSyntax classDeclarationSyntax,
        out ConstructorDeclarationSyntax? constructor)
    {
        constructor = null;
        var allConstructors = classDeclarationSyntax.Members.Where(member => member is ConstructorDeclarationSyntax)
            .Cast<ConstructorDeclarationSyntax>()
            .ToList();
        if (allConstructors.HasAny())
        {
            foreach (var ctor in allConstructors)
            {
                if (!ctor.IsPrivateInstanceConstructor())
                {
                    constructor = ctor;
                    return false;
                }
            }
        }

        return true;
    }

    public static bool HasRouteAttribute(this ClassDeclarationSyntax classDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var attribute =
            classDeclarationSyntax.GetAttributeOfType<RouteAttribute>(context.SemanticModel, context.Compilation);
        return attribute.Exists();
    }

    public static bool IsNamedEndingIn(this ClassDeclarationSyntax classDeclarationSyntax, string name)
    {
        var className = classDeclarationSyntax.Identifier.Text;
        return className.EndsWith(name);
    }

    public static bool IsOptionalType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var propertySymbol = CSharpExtensions.GetDeclaredSymbol(context.SemanticModel, propertyDeclarationSyntax);
        if (propertySymbol is null)
        {
            return false;
        }

        var getter = propertySymbol.GetMethod;
        if (getter is null)
        {
            return false;
        }

        var returnType = propertySymbol.GetMethod!.ReturnType;
        if (returnType.IsOptionalType(context))
        {
            return true;
        }

        return false;
    }

    public static bool IsOptionalType(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        var optionalType = context.Compilation.GetTypeByMetadataName(typeof(global::Common.Optional<>).FullName!)!;

        return symbol.OriginalDefinition.IsOfType(optionalType);
    }

    public static bool IsWithinNamespace(this ClassDeclarationSyntax classDeclarationSyntax,
        SyntaxNodeAnalysisContext context, string ns)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (symbol is null)
        {
            return false;
        }

        var containingNamespace = symbol.ContainingNamespace.ToDisplayString();
        return containingNamespace.StartsWith(ns);
    }

    public static ITypeSymbol WithoutOptional(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
    {
        if (symbol.IsOptionalType(context))
        {
            return ((INamedTypeSymbol)symbol).TypeArguments[0];
        }

        return symbol;
    }
}