using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Common.Extensions;

public static class SyntaxExtensions
{
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

    public static XmlNodeSyntax? SelectSingleElement(this SyntaxList<XmlNodeSyntax> content, string elementName)
    {
        return SelectElements(content, elementName)
            .FirstOrDefault();
    }

    public static List<XmlNodeSyntax> SelectElements(this SyntaxList<XmlNodeSyntax> content, string elementName)
    {
        return GetXmlElements(content, elementName)
            .ToList();
    }

    public static ClassDeclarationSyntax InsertMember(this ClassDeclarationSyntax classDeclarationSyntax, int index,
        MemberDeclarationSyntax newMember)
    {
        return classDeclarationSyntax.WithMembers(classDeclarationSyntax.Members.Insert(index, newMember));
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