using System.Collections.Immutable;
using Common.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tools.Analyzers.Common;
using Tools.Analyzers.Common.Extensions;
using Tools.Analyzers.Platform.Extensions;

namespace Tools.Analyzers.Platform;

/// <summary>
///     An analyzer to find public declarations that are missing a documentation &lt;summary&gt; node.
///     SAASDOC001: All public/internal classes, structs, records, interfaces, delegates and enums
///     SAASDOC002: All public/internal static methods and all public/internal extension methods (in public types)
///     Note: Document declarations are only enforced for Platform projects.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingDocsAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule001 = "SAASDOC001".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Documentation, nameof(Resources.SAASDOC001Title),
        nameof(Resources.SAASDOC001Description),
        nameof(Resources.SAASDOC001MessageFormat));

    internal static readonly DiagnosticDescriptor Rule002 = "SAASDOC002".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Documentation, nameof(Resources.SAASDOC002Title),
        nameof(Resources.SAASDOC002Description),
        nameof(Resources.SAASDOC002MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule001, Rule002);

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
        if (typeSyntax is not MemberDeclarationSyntax memberDeclarationSyntax) //class, struct, interface, record
        {
            return;
        }

        if (!context.IsIncludedInNamespace(memberDeclarationSyntax, AnalyzerConstants.PlatformNamespaces))
        {
            return;
        }

        if (memberDeclarationSyntax.IsNotPublicNorInternalInstanceType())
        {
            return;
        }

        if (memberDeclarationSyntax.IsNestedAndNotPublicType())
        {
            return;
        }

        var docs = memberDeclarationSyntax.GetDocumentationCommentTriviaSyntax(context);
        if (docs.NotExists()
            || !docs.IsLanguageForCSharp())
        {
            context.ReportDiagnostic(Rule001, memberDeclarationSyntax);
            return;
        }

        var xmlContent = docs.Content;
        var inheritdoc = xmlContent.SelectSingleElement(AnalyzerConstants.XmlDocumentation.Elements.InheritDoc);
        if (inheritdoc is not null)
        {
            return;
        }

        var summary = xmlContent.SelectSingleElement(AnalyzerConstants.XmlDocumentation.Elements.Summary);
        if (summary.NotExists()
            || summary.IsEmptyNode())
        {
            context.ReportDiagnostic(Rule001, memberDeclarationSyntax);
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = context.Node;
        if (methodSyntax is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (!context.IsIncludedInNamespace(methodDeclarationSyntax, AnalyzerConstants.PlatformNamespaces))
        {
            return;
        }

        if (methodDeclarationSyntax.IsParentTypeNotPublic())
        {
            return;
        }

        if (methodDeclarationSyntax.IsParentTypeNotStatic())
        {
            return;
        }

        if (methodDeclarationSyntax.IsNotPublicOrInternalStaticMethod())
        {
            return;
        }

        var docs = methodDeclarationSyntax.GetDocumentationCommentTriviaSyntax(context);
        if (docs is null)
        {
            context.ReportDiagnostic(Rule002, methodDeclarationSyntax);
            return;
        }

        if (!docs.IsLanguageForCSharp())
        {
            context.ReportDiagnostic(Rule002, methodDeclarationSyntax);
            return;
        }

        var xmlContent = docs.Content;
        var inheritdoc = xmlContent.SelectSingleElement(AnalyzerConstants.XmlDocumentation.Elements.InheritDoc);
        if (inheritdoc is not null)
        {
            return;
        }

        var summary = xmlContent.SelectSingleElement(AnalyzerConstants.XmlDocumentation.Elements.Summary);
        if (summary is null)
        {
            context.ReportDiagnostic(Rule002, methodDeclarationSyntax);
            return;
        }

        if (summary.IsEmptyNode())
        {
            context.ReportDiagnostic(Rule002, methodDeclarationSyntax);
        }
    }
}