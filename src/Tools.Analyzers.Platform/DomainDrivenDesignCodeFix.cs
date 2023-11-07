using System.Collections.Immutable;
using System.Composition;
using Common.Extensions;
using Domain.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Tools.Analyzers.Platform;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DomainDrivenDesignCodeFix))]
public class DomainDrivenDesignCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DomainDrivenDesignAnalyzer.Sas034.Id,
            DomainDrivenDesignAnalyzer.Sas043.Id,
            DomainDrivenDesignAnalyzer.Sas053.Id);

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

        if (syntax is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        var diagnostics = context.Diagnostics;
        if (diagnostics.Any(d => d.Id == DomainDrivenDesignAnalyzer.Sas034.Id))
        {
            var title = Resources.SAS022CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddRehydrateMethodToAggregateRoot(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d => d.Id == DomainDrivenDesignAnalyzer.Sas043.Id))
        {
            var title = Resources.SAS022CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddRehydrateMethodToEntity(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d => d.Id == DomainDrivenDesignAnalyzer.Sas053.Id))
        {
            var title = Resources.SAS022CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddRehydrateMethodToValueObject(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }
    }

    private static async Task<Solution> AddRehydrateMethodToAggregateRoot(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var compilation = await document.Project.GetCompilationAsync(cancellationToken);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

        var isDehydratable = semanticModel!.IsDehydratableAggregateRoot(compilation!, classDeclarationSyntax);

        var className = classDeclarationSyntax.Identifier.Text;
        var newMethod = CreateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            nameof(IRehydratableObject.Rehydrate),
            $"AggregateRootFactory<{className}>",
            isDehydratable.IsDehydratable
                ? $"return (identifier, container, properties) => new {className}(identifier, container, properties);"
                : $"return (identifier, container, properties) => new {className}(container.Resolve<IRecorder>(),container.Resolve<IIdentifierFactory>(), identifier);");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddRehydrateMethodToEntity(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var className = classDeclarationSyntax.Identifier.Text;
        var newMethod = CreateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            nameof(IRehydratableObject.Rehydrate),
            $"EntityFactory<{className}>",
            $"return (identifier, container, properties) => new {className}(identifier, container, properties);");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddRehydrateMethodToValueObject(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var compilation = await document.Project.GetCompilationAsync(cancellationToken);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

        var isSingleValueObject = semanticModel!.IsSingleValueValueObject(compilation!, classDeclarationSyntax);

        var className = classDeclarationSyntax.Identifier.Text;
        var newMethod = CreateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            nameof(IRehydratableObject.Rehydrate),
            $"ValueObjectFactory<{className}>",
            isSingleValueObject
                ? $"return (property, container) =>\n{{\nvar parts = RehydrateToList(property, true);\nreturn new {className}(parts[0]);\n}};"
                : $"return (property, container) =>\n{{\nvar parts = RehydrateToList(property, false);\nreturn new {className}(parts[0], parts[1], parts[2]);\n}};");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static MethodDeclarationSyntax CreateMethod(SyntaxKind[] accessibility, string methodName,
        string returnType, string bodyWithoutBlock)
    {
        var modifiers = accessibility.HasAny()
            ? SyntaxFactory.TokenList(accessibility.Select(SyntaxFactory.Token))
            : SyntaxFactory.TokenList();
        var bodySyntax = (BlockSyntax)SyntaxFactory.ParseStatement($"{{\n{bodyWithoutBlock}\n}}");
        return SyntaxFactory.MethodDeclaration(SyntaxFactory.List<AttributeListSyntax>(),
                modifiers,
                SyntaxFactory.ParseTypeName(returnType),
                null!,
                SyntaxFactory.Identifier(methodName),
                null!,
                SyntaxFactory.ParameterList(),
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                bodySyntax,
                null!)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }
}