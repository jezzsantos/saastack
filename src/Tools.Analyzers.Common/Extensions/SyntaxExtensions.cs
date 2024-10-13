using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Common.Extensions;

public static class SyntaxExtensions
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

    public static string GetContainingNamespace(this InterfaceDeclarationSyntax interfaceDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);
        if (symbol is null)
        {
            return string.Empty;
        }

        return symbol.ContainingNamespace.ToDisplayString();
    }

    /// <summary>
    ///     Returns all the content in the XML node, and replaces newlines with spaces
    /// </summary>
    public static string? GetContent(this XmlNodeSyntax? nodeSyntax)
    {
        if (nodeSyntax is null)
        {
            return null;
        }

        if (nodeSyntax is XmlTextSyntax textSyntax)
        {
            var content = string.Join(string.Empty, textSyntax.TextTokens
                .Where(tok => !string.IsNullOrWhiteSpace(tok.ToFullString()))
                .Select(tok => tok.ToString()));

            return content.TrimStart('\t', ' ').TrimEnd('\t', ' ');
        }

        if (nodeSyntax is XmlElementSyntax xmlElementSyntax)
        {
            var content = xmlElementSyntax.Content;
            return string.Join(" ", content.Select(GetContent));
        }

        return null;
    }

    public static DocumentationCommentTriviaSyntax? GetDocumentationCommentTriviaSyntax(this SyntaxNode node,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(node);
        if (symbol is null)
        {
            return null;
        }

        IEnumerable<SyntaxNode> syntaxes = new List<SyntaxNode> { node };
        if (node is ClassDeclarationSyntax classDeclarationSyntax)
        {
            if (classDeclarationSyntax.IsPartialClass())
            {
                syntaxes = symbol.DeclaringSyntaxReferences.Select(x => x.GetSyntax());
            }
        }

        var trivia = syntaxes
            .SelectMany(syntax => syntax.GetLeadingTrivia())
            .Select(leadingTrivia => leadingTrivia.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .FirstOrDefault();

        return trivia;
    }

    public static ITypeSymbol? GetGetterReturnType(this PropertyDeclarationSyntax propertyDeclarationSyntax,
        SyntaxNodeAnalysisContext context)
    {
        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclarationSyntax);
        if (propertySymbol is null)
        {
            return null;
        }

        var getter = propertySymbol.GetMethod;
        if (getter is null)
        {
            return null;
        }

        return propertySymbol.GetMethod!.ReturnType;
    }

    public static ClassDeclarationSyntax InsertMember(this ClassDeclarationSyntax classDeclarationSyntax, int index,
        MemberDeclarationSyntax newMember)
    {
        return classDeclarationSyntax.WithMembers(classDeclarationSyntax.Members.Insert(index, newMember));
    }

    public static List<XmlNodeSyntax> SelectElements(this SyntaxList<XmlNodeSyntax> content, string elementName)
    {
        return GetXmlElements(content, elementName)
            .ToList();
    }

    public static XmlNodeSyntax? SelectSingleElement(this SyntaxList<XmlNodeSyntax> content, string elementName)
    {
        return SelectElements(content, elementName)
            .FirstOrDefault();
    }

    private static IEnumerable<XmlNodeSyntax> GetXmlElements(this SyntaxList<XmlNodeSyntax> content, string elementName)
    {
        foreach (var syntax in content)
        {
            if (syntax is XmlEmptyElementSyntax emptyElement)
            {
                if (string.Equals(elementName, emptyElement.Name.ToString(), StringComparison.Ordinal))
                {
                    yield return emptyElement;
                }

                continue;
            }

            if (syntax is XmlElementSyntax elementSyntax)
            {
                if (string.Equals(elementName, elementSyntax.StartTag?.Name?.ToString(), StringComparison.Ordinal))
                {
                    yield return elementSyntax;
                }
            }
        }
    }
}