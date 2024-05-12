using System.Collections.Immutable;
using System.Composition;
using Common.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Task = System.Threading.Tasks.Task;

namespace Tools.Analyzers.NonPlatform;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ApiLayerCodeFix))]
public class ApiLayerCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            ApiLayerAnalyzer.Rule039.Id
        );

    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if (root.NotExists())
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var syntax = root.FindNode(diagnosticSpan);
        if (syntax.NotExists())
        {
            return;
        }

        if (syntax is ClassDeclarationSyntax classDeclarationSyntax)
        {
            FixRequestClass(context, classDeclarationSyntax);
        }
    }

    private static void FixRequestClass(CodeFixContext context, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var diagnostics = context.Diagnostics;
        var diagnostic = diagnostics.First();

        if (diagnostics.Any(d => d.Id == ApiLayerAnalyzer.Rule039.Id))
        {
            var title = Resources.CodeFix_ApiLayer_Title_AddDocumentSummary;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddDocumentation(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }
    }

    private static async Task<Solution> AddDocumentation(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var firstModifier = classDeclarationSyntax.Modifiers.First();
        var hasAttributes = classDeclarationSyntax.AttributeLists.HasAny();
        var syntaxKind = hasAttributes
            ? SyntaxKind.OpenBracketToken //We append to the first attribute open square bracket
            : firstModifier.Kind(); //We append to class modifier (i.e. public)

        var documentation =
            SyntaxFactory.Token(
                SyntaxFactory.ParseLeadingTrivia(
                    @"/// <summary>
///  A summary
/// </summary>
/// <response code=""405"">A description of an optional specific error response (if desired)</response>
"),
                syntaxKind,
                SyntaxFactory.TriviaList());

        ClassDeclarationSyntax modifiedClassDeclaration;
        if (hasAttributes)
        {
            var attributes = SyntaxFactory.List(new[]
                {
                    classDeclarationSyntax.AttributeLists.First()
                        .WithOpenBracketToken(documentation)
                }
                .Concat(classDeclarationSyntax.AttributeLists.Skip(1))
                .ToArray());

            modifiedClassDeclaration = classDeclarationSyntax
                .WithAttributeLists(attributes)
                .WithModifiers(SyntaxFactory.TokenList(firstModifier))
                .NormalizeWhitespace();
        }
        else
        {
            modifiedClassDeclaration = classDeclarationSyntax
                .WithModifiers(SyntaxFactory.TokenList(documentation))
                .NormalizeWhitespace();
        }

        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }
}