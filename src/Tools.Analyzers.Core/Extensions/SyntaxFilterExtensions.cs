using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Core.Extensions;

internal static class SyntaxFilterExtensions
{
    public static AttributeData? GetAttributeOfType<TAttribute>(this MethodDeclarationSyntax methodDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
        if (symbol is null)
        {
            return null;
        }

        var attributeType = context.Compilation.GetTypeByMetadataName(typeof(TAttribute).FullName!)!;
        var attributes = symbol.GetAttributes();

        return attributes.FirstOrDefault(attr => attr.AttributeClass!.IsOfType(attributeType));
    }

    public static ITypeSymbol? GetBaseOfType<TType>(this ParameterSyntax parameterSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(parameterSyntax);
        if (symbol is null)
        {
            return null;
        }

        var parameterType = context.Compilation.GetTypeByMetadataName(typeof(TType).FullName!)!;

        var isOfType = symbol.Type.IsOfType(parameterType);
        if (isOfType)
        {
            return null;
        }

        var isDerivedFrom = symbol.Type.AllInterfaces.Any(@interface => @interface.IsOfType(parameterType));
        if (isDerivedFrom)
        {
            return symbol.Type;
        }

        return null;
    }

    public static bool IsEmptyNode(this XmlNodeSyntax nodeSyntax)
    {
        if (nodeSyntax is XmlTextSyntax textSyntax)
        {
            return textSyntax.TextTokens.All(token => string.IsNullOrWhiteSpace(token.ToString()));
        }

        if (nodeSyntax is XmlElementSyntax xmlElementSyntax)
        {
            var content = xmlElementSyntax.Content;
            return content.All(IsEmptyNode);
        }

        return true;
    }

    public static bool IsExcludedInNamespace(this SyntaxNodeAnalysisContext context, string[] excludedNamespaces)
    {
        var parentContext = context.ContainingSymbol;
        if (parentContext is null)
        {
            return true;
        }

        var containingNamespace = parentContext.ContainingNamespace.ToDisplayString();
        var excluded = excludedNamespaces.Contains(containingNamespace);

        return excluded;
    }

    public static bool IsIncludedInNamespace(this SyntaxNodeAnalysisContext context, string[] includedNamespaces)
    {
        var parentContext = context.ContainingSymbol;
        if (parentContext is null)
        {
            return true;
        }

        var containingNamespace = parentContext.ContainingNamespace.ToDisplayString();
        var included = includedNamespaces.Contains(containingNamespace);

        return included;
    }

    public static bool IsLanguageForCSharp(this SyntaxNode docs)
    {
        return docs.Language == "C#";
    }

    public static bool IsNestedAndNotPublicType(this MemberDeclarationSyntax memberDeclaration)
    {
        var isNested = memberDeclaration.Parent.IsKind(SyntaxKind.ClassDeclaration);
        if (!isNested)
        {
            return false;
        }

        var accessibility = new Accessibility(memberDeclaration.Modifiers);
        if (accessibility.IsPublic)
        {
            return false;
        }

        return true;
    }

    public static bool IsNotPublicInstanceMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var accessibility = new Accessibility(methodDeclarationSyntax.Modifiers);
        return accessibility is { IsPublic: false };
    }

    public static bool IsNotPublicNorInternalInstanceType(this MemberDeclarationSyntax memberDeclaration)
    {
        var accessibility = new Accessibility(memberDeclaration.Modifiers);
        if (accessibility is { IsPublic: false, IsInternal: false })
        {
            return true;
        }

        if (accessibility.IsStatic)
        {
            return true;
        }

        return false;
    }

    public static bool IsNotPublicOrInternalStaticMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var accessibility = new Accessibility(methodDeclarationSyntax.Modifiers);
        if (accessibility is { IsPublic: false, IsInternal: false })
        {
            return true;
        }

        if (!accessibility.IsStatic)
        {
            return true;
        }

        return false;
    }

    public static bool IsNotType<TParent>(this ClassDeclarationSyntax classDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        if (symbol is null)
        {
            return false;
        }

        var parentType = context.Compilation.GetTypeByMetadataName(typeof(TParent).FullName!)!;

        var isOfType = symbol.AllInterfaces.Any(@interface => @interface.IsOfType(parentType));

        return !isOfType;
    }

    public static bool IsNotType<TType>(this ParameterSyntax parameterSyntax, SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(parameterSyntax);
        if (symbol is null)
        {
            return false;
        }

        var parameterType = context.Compilation.GetTypeByMetadataName(typeof(TType).FullName!)!;

        var isOfType = symbol.Type.IsOfType(parameterType);
        if (isOfType)
        {
            return false;
        }

        var isDerivedFrom = symbol.Type.AllInterfaces.Any(@interface => @interface.IsOfType(parameterType));

        return !isDerivedFrom;
    }

    public static bool IsParentTypeNotPublic(this MemberDeclarationSyntax memberDeclaration)
    {
        var parent = memberDeclaration.Parent;
        if (parent is not BaseTypeDeclarationSyntax typeDeclaration)
        {
            return false;
        }

        var accessibility = new Accessibility(typeDeclaration.Modifiers);
        if (accessibility.IsPublic)
        {
            return false;
        }

        return true;
    }

    // ReSharper disable once UnusedMember.Local
    public static bool IsPublicOrInternalExtensionMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var isNotPublicOrInternal = IsNotPublicOrInternalStaticMethod(methodDeclarationSyntax);
        if (isNotPublicOrInternal)
        {
            return false;
        }

        var firstParameter = methodDeclarationSyntax.ParameterList.Parameters.FirstOrDefault();
        if (firstParameter is null)
        {
            return false;
        }

        var isExtension = firstParameter.Modifiers.Any(mod => mod.IsKind(SyntaxKind.ThisKeyword));
        if (!isExtension)
        {
            return false;
        }

        return true;
    }
}

public class Accessibility
{
    public Accessibility(SyntaxTokenList modifiers)
    {
        IsPublic = modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword));
        IsInternal = modifiers.Any(mod => mod.IsKind(SyntaxKind.InternalKeyword));
        IsStatic = modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword));
    }

    public bool IsInternal { get; }

    public bool IsPublic { get; }

    public bool IsStatic { get; }
}