using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tools.Analyzers.Core.Extensions;

namespace Tools.Analyzers.Core;

/// <summary>
///     An analyzer to find public declarations that are missing a documentation &lt;summary&gt; node.
///     SAS001: All public/internal classes, structs, records, interfaces, delegates and enums
///     SAS002: All public/internal static methods and all public/internal extension methods (in public types)
///     Document declarations are only enforced for Core common/interfaces projects.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MissingDocsAnalyzer : DiagnosticAnalyzer
{
    private const string SummaryXmlElementName = "summary";

    internal static readonly DiagnosticDescriptor Sas001 = "SAS001".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Documentation, nameof(Resources.SAS001Title), nameof(Resources.SAS001Description),
        nameof(Resources.SAS001MessageFormat));

    internal static readonly DiagnosticDescriptor Sas002 = "SAS002".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.Documentation, nameof(Resources.SAS002Title), nameof(Resources.SAS002Description),
        nameof(Resources.SAS002MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Sas001, Sas002);

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

        if (!context.IsIncludedInNamespace(AnalyzerConstants.CommonNamespaces))
        {
            return;
        }

        if (typeDeclarationSyntax.IsNotPublicNorInternalInstanceType())
        {
            return;
        }

        if (typeDeclarationSyntax.IsNestedAndNotPublicType())
        {
            return;
        }

        var docs = typeDeclarationSyntax.GetDocumentationCommentTriviaSyntax();
        if (docs is null)
        {
            context.ReportDiagnostic(Sas001, typeDeclarationSyntax);
            return;
        }

        if (!docs.IsLanguageForCSharp())
        {
            context.ReportDiagnostic(Sas001, typeDeclarationSyntax);
            return;
        }

        var xmlContent = docs.Content;
        var summary = xmlContent.GetFirstXmlElement(SummaryXmlElementName);
        if (summary is null)
        {
            context.ReportDiagnostic(Sas001, typeDeclarationSyntax);
            return;
        }

        if (summary.IsEmptyNode())
        {
            context.ReportDiagnostic(Sas001, typeDeclarationSyntax);
        }
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = context.Node;
        if (methodSyntax is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (!context.IsIncludedInNamespace(AnalyzerConstants.CommonNamespaces))
        {
            return;
        }

        if (methodDeclarationSyntax.IsParentTypeNotPublic())
        {
            return;
        }

        if (methodDeclarationSyntax.IsNotPublicOrInternalStaticMethod())
        {
            return;
        }

        var docs = methodDeclarationSyntax.GetDocumentationCommentTriviaSyntax();
        if (docs is null)
        {
            context.ReportDiagnostic(Sas002, methodDeclarationSyntax);
            return;
        }

        if (!docs.IsLanguageForCSharp())
        {
            context.ReportDiagnostic(Sas002, methodDeclarationSyntax);
            return;
        }

        var xmlContent = docs.Content;
        var summary = xmlContent.GetFirstXmlElement(SummaryXmlElementName);
        if (summary is null)
        {
            context.ReportDiagnostic(Sas002, methodDeclarationSyntax);
            return;
        }

        if (summary.IsEmptyNode())
        {
            context.ReportDiagnostic(Sas002, methodDeclarationSyntax);
        }
    }
}