using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Tools.Analyzers.Common.Extensions;

public static class SyntaxExtensions
{
    public static DocumentationCommentTriviaSyntax? GetDocumentationCommentTriviaSyntax(this SyntaxNode node)
    {
        foreach (var leadingTrivia in node.GetLeadingTrivia())
        {
            if (leadingTrivia.GetStructure() is DocumentationCommentTriviaSyntax structure)
            {
                return structure;
            }
        }

        return null;
    }

    public static XmlNodeSyntax? GetFirstXmlElement(this SyntaxList<XmlNodeSyntax> content, string elementName)
    {
        return GetXmlElements(content, elementName)
            .FirstOrDefault();
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