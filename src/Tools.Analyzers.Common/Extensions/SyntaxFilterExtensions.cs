using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Common.Extensions;

public static class SyntaxFilterExtensions
{
    public static bool HasParameterlessConstructor(this ClassDeclarationSyntax classDeclarationSyntax)
    {
        var allConstructors = classDeclarationSyntax.Members.Where(member => member is ConstructorDeclarationSyntax)
            .Cast<ConstructorDeclarationSyntax>()
            .ToList();
        if (allConstructors.Count > 0)
        {
            var parameterlessConstructors = allConstructors
                .Where(constructor => constructor.ParameterList.Parameters.Count == 0 && constructor.IsPublic());
            if (!parameterlessConstructors.Any())
            {
                return false;
            }
        }

        return true;
    }

    public static bool HasPublicGetterAndSetter(this PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        var accessors = propertyDeclarationSyntax.AccessorList;
        if (accessors is null)
        {
            return false;
        }

        var setter = accessors.Accessors.FirstOrDefault(accessor =>
            accessor.IsKind(SyntaxKind.SetAccessorDeclaration));
        if (setter is null)
        {
            return false;
        }

        if (setter.Body is not null)
        {
            return false;
        }

        if (setter.Modifiers.Any())
        {
            var setterAccessibility = new Accessibility(setter.Modifiers);
            if (!setterAccessibility.IsPublic || setterAccessibility.IsStatic)
            {
                return false;
            }
        }

        var getter = accessors.Accessors.FirstOrDefault(accessor =>
            accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
        if (getter is null)
        {
            return false;
        }

        if (getter.Body is not null)
        {
            return false;
        }

        if (getter.Modifiers.Any())
        {
            var getterAccessibility = new Accessibility(getter.Modifiers);
            if (!getterAccessibility.IsPublic || getterAccessibility.IsStatic)
            {
                return false;
            }
        }

        return true;
    }

    public static bool HasPublicGetterOnly(this PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        var accessors = propertyDeclarationSyntax.AccessorList;
        if (accessors is null)
        {
            return false;
        }

        var getter = accessors.Accessors.FirstOrDefault(accessor =>
            accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
        if (getter is null)
        {
            return false;
        }

        if (getter.Body is not null)
        {
            return false;
        }

        if (getter.Modifiers.Any())
        {
            var getterAccessibility = new Accessibility(getter.Modifiers);
            if (!getterAccessibility.IsPublic || getterAccessibility.IsStatic)
            {
                return false;
            }
        }

        var setter = accessors.Accessors.FirstOrDefault(accessor =>
            accessor.IsKind(SyntaxKind.SetAccessorDeclaration));
        if (setter is null)
        {
            return true;
        }

        if (setter.Modifiers.Any())
        {
            var setterAccessibility = new Accessibility(setter.Modifiers);
            if (setterAccessibility is { IsPrivate: true, IsStatic: false })
            {
                return true;
            }
        }

        return false;
    }

    public static bool HasPublicSetter(this PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        var accessors = propertyDeclarationSyntax.AccessorList;
        if (accessors is null)
        {
            return false;
        }

        var setter = accessors.Accessors.FirstOrDefault(accessor =>
            accessor.IsKind(SyntaxKind.SetAccessorDeclaration));
        if (setter is null)
        {
            return false;
        }

        if (setter.Modifiers.Any())
        {
            var setterAccessibility = new Accessibility(setter.Modifiers);
            if (!setterAccessibility.IsPublic || setterAccessibility.IsStatic)
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsDtoOrNullableDto(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context, List<INamedTypeSymbol> allowableTypes)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
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
        if (returnType.IsNullable(context))
        {
            if (IsDto(returnType.WithoutNullable(context)))
            {
                return true;
            }
        }

        if (IsDto(returnType))
        {
            return true;
        }

        return false;

        bool IsDto(ITypeSymbol symbol)
        {
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return false;
            }

            if (!namedTypeSymbol.IsReferenceType) //We dont accept any enums, or other value types
            {
                return false;
            }

            if (namedTypeSymbol.IsStatic
                || namedTypeSymbol.DeclaredAccessibility != Microsoft.CodeAnalysis.Accessibility.Public)
            {
                return false;
            }

            if (!namedTypeSymbol.HasParameterlessConstructor())
            {
                return false;
            }

            if (!namedTypeSymbol.HasPublicGetterAndSetterProperties())
            {
                return false;
            }

            if (!namedTypeSymbol.HasPropertiesOfAllowableTypes(allowableTypes))
            {
                return false;
            }

            return true;
        }
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

    public static bool IsEnumOrNullableEnumType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
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
        if (returnType.IsNullable(context))
        {
            if (returnType.WithoutNullable(context).IsEnum())
            {
                return true;
            }
        }

        if (returnType.IsEnum())
        {
            return true;
        }

        return false;
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
    

    public static bool IsInitialized(this PropertyDeclarationSyntax propertyDeclarationSyntax)
    {
        return propertyDeclarationSyntax.Initializer is not null;
    }

    public static bool IsLanguageForCSharp(this SyntaxNode docs)
    {
        return docs.Language == "C#";
    }

    public static bool IsNamed(this MethodDeclarationSyntax methodDeclarationSyntax, string name)
    {
        var methodName = methodDeclarationSyntax.Identifier.Text;
        return string.Equals(name, methodName, StringComparison.OrdinalIgnoreCase);
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

    public static bool IsNotType<TParent>(this InterfaceDeclarationSyntax interfaceDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);
        if (symbol is null)
        {
            return false;
        }

        var parentMetadata = context.Compilation.GetTypeByMetadataName(typeof(TParent).FullName!)!;

        return !symbol.AllInterfaces.Any(@interface => @interface.IsOfType(parentMetadata));
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

        return !symbol.AllInterfaces.Any(@interface => @interface.IsOfType(parentMetadata));
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

    public static bool IsNullableType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
        if (propertySymbol is null)
        {
            return false;
        }

        var getter = propertySymbol.GetMethod;
        if (getter is null)
        {
            return false;
        }

        var returnType = getter.ReturnType;
        if (returnType.IsNullable(context))
        {
            return true;
        }

        return false;
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

    public static bool IsParentTypeNotStatic(this MemberDeclarationSyntax memberDeclaration)
    {
        var parent = memberDeclaration.Parent;
        if (parent is not BaseTypeDeclarationSyntax classDeclaration)
        {
            return false;
        }

        var accessibility = new Accessibility(classDeclaration.Modifiers);
        if (!accessibility.IsStatic)
        {
            return true;
        }

        return false;
    }

    public static bool IsPartialClass(this ClassDeclarationSyntax classDeclaration)
    {
        var accessibility = new Accessibility(classDeclaration.Modifiers);
        if (accessibility.IsPartial)
        {
            return true;
        }

        return false;
    }

    public static bool IsPrivateInstanceConstructor(this ConstructorDeclarationSyntax constructorDeclarationSyntax)
    {
        var accessibility = new Accessibility(constructorDeclarationSyntax.Modifiers);
        return accessibility is { IsPrivate: true, IsStatic: false };
    }

    public static bool IsPublic(this MemberDeclarationSyntax memberDeclarationSyntax)
    {
        var accessibility = new Accessibility(memberDeclarationSyntax.Modifiers);
        if (accessibility.IsPublic)
        {
            return true;
        }

        return false;
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

    public static bool IsRequired(this MemberDeclarationSyntax memberDeclarationSyntax)
    {
        var accessibility = new Accessibility(memberDeclarationSyntax.Modifiers);
        if (accessibility.IsRequired)
        {
            return true;
        }

        return false;
    }

    public static bool IsSealed(this ClassDeclarationSyntax classDeclaration)
    {
        var accessibility = new Accessibility(classDeclaration.Modifiers);
        if (accessibility.IsSealed)
        {
            return true;
        }

        return false;
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
        IsPartial = modifiers.Any(mod => mod.IsKind(SyntaxKind.PartialKeyword));
        IsSealed = modifiers.Any(mod => mod.IsKind(SyntaxKind.SealedKeyword));
        IsRequired = modifiers.Any(mod => mod.IsKind(SyntaxKind.RequiredKeyword));
    }

    public bool IsInternal { get; }

    public bool IsPartial { get; }

    public bool IsPrivate { get; }

    public bool IsPublic { get; }

    public bool IsRequired { get; }

    public bool IsSealed { get; }

    public bool IsStatic { get; }
}