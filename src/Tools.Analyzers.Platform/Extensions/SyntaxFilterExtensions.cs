using Common.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Platform.Extensions;

internal static class SyntaxFilterExtensions
{
    public static AttributeData? GetAttributeOfType<TAttribute>(this MemberDeclarationSyntax memberDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        return GetAttributeOfType<TAttribute>(memberDeclarationSyntax, context.SemanticModel, context.Compilation);
    }

    public static AttributeData? GetAttributeOfType<TAttribute>(this MemberDeclarationSyntax memberDeclarationSyntax,
        SemanticModel semanticModel, Compilation compilation)
    {
        var symbol = semanticModel.GetDeclaredSymbol(memberDeclarationSyntax);
        if (symbol is null)
        {
            return null;
        }

        return symbol.GetAttributeOfType<TAttribute>(compilation);
    }

    public static AttributeData? GetAttributeOfType<TAttribute>(this ISymbol? symbol,
        SyntaxNodeAnalysisContext context)
    {
        return GetAttributeOfType<TAttribute>(symbol, context.Compilation);
    }

    public static ITypeSymbol? GetBaseOfType<TType>(this ParameterSyntax parameterSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(parameterSyntax);
        if (symbol is null)
        {
            return null;
        }

        var parameterMetadata = context.Compilation.GetTypeByMetadataName(typeof(TType).FullName!)!;
        var isOfType = symbol.Type.IsOfType(parameterMetadata);
        if (isOfType)
        {
            return null;
        }

        var isDerivedFrom = symbol.Type.AllInterfaces.Any(@interface => @interface.IsOfType(parameterMetadata));
        if (isDerivedFrom)
        {
            return symbol.Type;
        }

        return null;
    }

    public static bool HasPublicSetter(this PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        var propertyAccessibility = new Accessibility(propertyDeclarationSyntax.Modifiers);
        var isPublicProperty = propertyAccessibility.IsPublic;

        var accessors = propertyDeclarationSyntax.AccessorList;
        if (accessors.NotExists())
        {
            return false;
        }

        var setter = accessors.Accessors.FirstOrDefault(accessor =>
            accessor.IsKind(SyntaxKind.SetAccessorDeclaration));
        if (setter.NotExists())
        {
            return false;
        }

        if (setter.Modifiers.HasNone())
        {
            return isPublicProperty;
        }

        var setterAccessibility = new Accessibility(setter.Modifiers);
        return setterAccessibility is { IsPublic: true, IsStatic: false };
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

    public static bool IsExcludedInNamespace(this SyntaxNodeAnalysisContext context,
        MemberDeclarationSyntax memberDeclarationSyntax, string[] excludedNamespaces)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(memberDeclarationSyntax);
        if (symbol is null)
        {
            return false;
        }

        var memberNamespace = symbol.ContainingNamespace.ToDisplayString();
        return excludedNamespaces.Any(ns => memberNamespace.StartsWith(ns));
    }

    public static bool IsIncludedInNamespace(this SyntaxNodeAnalysisContext context,
        MemberDeclarationSyntax memberDeclarationSyntax, string[] includedNamespaces)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(memberDeclarationSyntax);
        if (symbol is null)
        {
            return false;
        }

        var memberNamespace = symbol.ContainingNamespace.ToDisplayString();
        return includedNamespaces.Any(ns => memberNamespace.StartsWith(ns));
    }

    public static bool IsLanguageForCSharp(this SyntaxNode docs)
    {
        return docs.Language == "C#";
    }

    public static bool IsNamed(this MethodDeclarationSyntax methodDeclarationSyntax, string name)
    {
        var methodName = methodDeclarationSyntax.Identifier.Text;
        return name.EqualsIgnoreCase(methodName);
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

        var parentMetadata = context.Compilation.GetTypeByMetadataName(typeof(TParent).FullName!)!;

        var isOfType = symbol.AllInterfaces.Any(@interface => @interface.IsOfType(parentMetadata));

        return !isOfType;
    }

    public static bool IsNotType<TType>(this ParameterSyntax parameterSyntax, SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(parameterSyntax);
        if (symbol is null)
        {
            return false;
        }

        var parameterMetadata = context.Compilation.GetTypeByMetadataName(typeof(TType).FullName!)!;

        var isOfType = symbol.Type.IsOfType(parameterMetadata);
        if (isOfType)
        {
            return false;
        }

        var isDerivedFrom = symbol.Type.AllInterfaces.Any(@interface => @interface.IsOfType(parameterMetadata));

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

    public static bool IsPrivateInstanceConstructor(this ConstructorDeclarationSyntax constructorDeclarationSyntax)
    {
        var accessibility = new Accessibility(constructorDeclarationSyntax.Modifiers);
        return accessibility is { IsPrivate: true, IsStatic: false };
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

    public static bool IsPublicOrInternalInstanceMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var accessibility = new Accessibility(methodDeclarationSyntax.Modifiers);
        if (accessibility is { IsPublic: false, IsInternal: false })
        {
            return false;
        }

        if (accessibility.IsStatic)
        {
            return false;
        }

        return true;
    }

    public static bool IsPublicOverrideMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var accessibility = new Accessibility(methodDeclarationSyntax.Modifiers);
        return accessibility is { IsPublic: true, IsStatic: false }
               && methodDeclarationSyntax.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OverrideKeyword));
    }

    public static bool IsPublicStaticMethod(this MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var accessibility = new Accessibility(methodDeclarationSyntax.Modifiers);
        return accessibility is { IsPublic: true, IsStatic: true };
    }

    private static AttributeData? GetAttributeOfType<TAttribute>(this ISymbol? symbol,
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
}

public class Accessibility
{
    public Accessibility(SyntaxTokenList modifiers)
    {
        IsPrivate = modifiers.Any(mod => mod.IsKind(SyntaxKind.PrivateKeyword));
        IsPublic = modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword));
        IsInternal = modifiers.Any(mod => mod.IsKind(SyntaxKind.InternalKeyword));
        IsStatic = modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword));
    }

    public bool IsInternal { get; }

    public bool IsPrivate { get; }

    public bool IsPublic { get; }

    public bool IsStatic { get; }
}