using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Tools.Analyzers.Core;

/// <summary>
///     An analyzer to find public declarations that are missing a documentation &lt;summary&gt; node.
///     Document declarations are only enforced for Core common/interfaces projects.
///     All public/internal classes, structs, records, interfaces, delegates and enums
///     All public/internal static methods and all public/internal extension methods (in public types)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingDocsAnalyzer : DiagnosticAnalyzer
{
    public const string ExtensionMethodDiagnosticId = "SAS002";
    private const string RoslynCategory = "Documentation";
    private const string SummaryXmlElementName = "summary";
    public const string TypeDiagnosticId = "SAS001";

    private static readonly string[] IncludedNamespaces =
    {
#if TESTINGONLY
        "<global namespace>",
#endif
        "Common", "UnitTesting.Common", "IntegrationTesting.Common",
        "Infrastructure.Common", "Infrastructure.Interfaces",
        "Infrastructure.Persistence.Common", "Infrastructure.Persistence.Interfaces",
        "Infrastructure.WebApi.Common", "Infrastructure.WebApi.Interfaces",
        "Domain.Common", "Domain.Interfaces", "Application.Common", "Application.Interfaces"
    };

    private static readonly DiagnosticDescriptor TypeRule = new(TypeDiagnosticId,
        GetResource(nameof(Resources.SAS001Title)), GetResource(nameof(Resources.SAS001MessageFormat)), RoslynCategory,
        DiagnosticSeverity.Warning, true, GetResource(nameof(Resources.SAS001Description)));

    private static readonly DiagnosticDescriptor ExtensionMethodRule = new(ExtensionMethodDiagnosticId,
        GetResource(nameof(Resources.SAS002Title)), GetResource(nameof(Resources.SAS002MessageFormat)), RoslynCategory,
        DiagnosticSeverity.Warning, true, GetResource(nameof(Resources.SAS002Description)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(TypeRule, ExtensionMethodRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.StructDeclaration, SyntaxKind.ClassDeclaration,
            SyntaxKind.InterfaceDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.DelegateDeclaration,
            SyntaxKind.EnumDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        var typeSyntax = context.Node;
        if (typeSyntax is not MemberDeclarationSyntax typeDeclarationSyntax) //class, struct, interface, record
        {
            return;
        }

        if (IsIgnoredNamespace(context))
        {
            return;
        }

        if (IsNotPublicNorInternalInstanceType(typeDeclarationSyntax))
        {
            return;
        }

        if (IsNestedAndNotPublicType(typeDeclarationSyntax))
        {
            return;
        }

        var docs = typeDeclarationSyntax.GetDocumentationCommentTriviaSyntax();
        if (docs is null)
        {
            ReportDiagnostic(context, typeDeclarationSyntax);
            return;
        }

        if (!IsXmlDocsForCSharp(docs))
        {
            ReportDiagnostic(context, typeDeclarationSyntax);
            return;
        }

        var xmlContent = docs.Content;
        var summary = xmlContent.GetFirstXmlElement(SummaryXmlElementName);
        if (summary is null)
        {
            ReportDiagnostic(context, typeDeclarationSyntax);
            return;
        }

        if (IsEmptyNode(summary))
        {
            ReportDiagnostic(context, typeDeclarationSyntax);
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = context.Node;
        if (methodSyntax is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (IsIgnoredNamespace(context))
        {
            return;
        }

        if (IsParentTypeNotPublic(methodDeclarationSyntax))
        {
            return;
        }

        if (IsNotPublicOrInternalStaticMethod(methodDeclarationSyntax))
        {
            return;
        }

        var docs = methodDeclarationSyntax.GetDocumentationCommentTriviaSyntax();
        if (docs is null)
        {
            ReportDiagnostic(context, methodDeclarationSyntax);
            return;
        }

        if (!IsXmlDocsForCSharp(docs))
        {
            ReportDiagnostic(context, methodDeclarationSyntax);
            return;
        }

        var xmlContent = docs.Content;
        var summary = xmlContent.GetFirstXmlElement(SummaryXmlElementName);
        if (summary is null)
        {
            ReportDiagnostic(context, methodDeclarationSyntax);
            return;
        }

        if (IsEmptyNode(summary))
        {
            ReportDiagnostic(context, methodDeclarationSyntax);
        }
    }

    private static LocalizableResourceString GetResource(string name)
    {
        return new LocalizableResourceString(name, Resources.ResourceManager, typeof(Resources));
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context,
        MemberDeclarationSyntax memberDeclarationSyntax)
    {
        var location = Location.None;
        var text = "Unknown";

        if (memberDeclarationSyntax is BaseTypeDeclarationSyntax baseType)
        {
            location = baseType.Identifier.GetLocation();
            text = baseType.Identifier.Text;
        }

        if (memberDeclarationSyntax is DelegateDeclarationSyntax delegateType)
        {
            location = delegateType.Identifier.GetLocation();
            text = delegateType.Identifier.Text;
        }

        var diagnostic = Diagnostic.Create(TypeRule, location, text);
        context.ReportDiagnostic(diagnostic);
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var identifier = methodDeclarationSyntax.Identifier;
        var diagnostic = Diagnostic.Create(ExtensionMethodRule, identifier.GetLocation(), identifier.Text);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsEmptyNode(XmlNodeSyntax nodeSyntax)
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

    private static bool IsIgnoredNamespace(SyntaxNodeAnalysisContext context)
    {
        var parentContext = context.ContainingSymbol;
        if (parentContext is null)
        {
            return true;
        }

        var containingNamespace = parentContext.ContainingNamespace.ToDisplayString();
        var included = IncludedNamespaces.Contains(containingNamespace);

        return !included;
    }

    private static bool IsNotPublicNorInternalInstanceType(MemberDeclarationSyntax memberDeclaration)
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

    private static bool IsNestedAndNotPublicType(MemberDeclarationSyntax memberDeclaration)
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

    private static bool IsParentTypeNotPublic(MemberDeclarationSyntax memberDeclaration)
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

    private static bool IsNotPublicOrInternalStaticMethod(MethodDeclarationSyntax methodDeclarationSyntax)
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

    private static bool IsPublicOrInternalExtensionMethod(MethodDeclarationSyntax methodDeclarationSyntax)
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

    private static bool IsXmlDocsForCSharp(SyntaxNode docs)
    {
        return docs.Language == "C#";
    }
}

internal static class SyntaxExtensions
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
        return content.GetXmlElements(elementName)
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