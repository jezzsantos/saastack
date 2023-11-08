using System.Collections.Immutable;
using System.Composition;
using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using QueryAny;
using Tools.Analyzers.Platform.Extensions;

namespace Tools.Analyzers.Platform;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DomainDrivenDesignCodeFix))]
public class DomainDrivenDesignCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DomainDrivenDesignAnalyzer.Sas030.Id,
            DomainDrivenDesignAnalyzer.Sas034.Id,
            DomainDrivenDesignAnalyzer.Sas035.Id,
            DomainDrivenDesignAnalyzer.Sas036.Id,
            DomainDrivenDesignAnalyzer.Sas040.Id,
            DomainDrivenDesignAnalyzer.Sas043.Id,
            DomainDrivenDesignAnalyzer.Sas044.Id,
            DomainDrivenDesignAnalyzer.Sas045.Id,
            DomainDrivenDesignAnalyzer.Sas050.Id,
            DomainDrivenDesignAnalyzer.Sas053.Id
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

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Sas035.Id))
        {
            var title = Resources.SAS023CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddDehydrateMethodToAggregateRoot(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Sas044.Id))
        {
            var title = Resources.SAS023CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddDehydrateMethodToEntity(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Sas030.Id))
        {
            var title = Resources.SAS030CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddCreateMethodToAggregateRoot(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Sas040.Id))
        {
            var title = Resources.SAS040CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddCreateMethodToEntity(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Sas050.Id))
        {
            var title = Resources.SAS050CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddCreateMethodToValueObject(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Sas036.Id || d.Id == DomainDrivenDesignAnalyzer.Sas045.Id))
        {
            var title = Resources.SAS024CodeFixTitle;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddEntityValueAttribute(context.Document, classDeclarationSyntax, token),
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
        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            nameof(IRehydratableObject.Rehydrate),
            $"Domain.Common.AggregateRootFactory<{className}>",
            null,
            isDehydratable.IsDehydratable
                ? $"return (identifier, container, properties) => new {className}(identifier, container, properties);"
                : $"return (identifier, container, properties) => new {className}(container.Resolve<IRecorder>(),container.Resolve<IIdentifierFactory>(), identifier);");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddCreateMethodToAggregateRoot(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var className = classDeclarationSyntax.Identifier.Text;
        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            DomainDrivenDesignAnalyzer.ClassFactoryMethodName,
            $"Result<{className}, Error>",
            new Dictionary<string, string>
            {
                { "recorder", nameof(IRecorder) },
                { "idFactory", nameof(IIdentifierFactory) },
                { "organizationId", nameof(Identifier) }
            },
            $"var root = new {className}(recorder, idFactory);\nroot.RaiseCreateEvent(Created.Create(root.Id, organizationId));\nreturn root;");
        var modifiedClassDeclaration = classDeclarationSyntax.InsertMember(0, newMethod);
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
        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            nameof(IRehydratableObject.Rehydrate),
            $"Domain.Common.EntityFactory<{className}>",
            null,
            $"return (identifier, container, properties) => new {className}(identifier, container, properties);");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddCreateMethodToEntity(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var className = classDeclarationSyntax.Identifier.Text;
        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            DomainDrivenDesignAnalyzer.ClassFactoryMethodName,
            $"Result<{className}, Error>",
            new Dictionary<string, string>
            {
                { "recorder", nameof(IRecorder) },
                { "idFactory", nameof(IIdentifierFactory) },
                { "rootEventHandler", nameof(RootEventHandler) }
            },
            $"return new {className}(recorder, idFactory, rootEventHandler);");
        var modifiedClassDeclaration = classDeclarationSyntax.InsertMember(0, newMethod);
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
        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            nameof(IRehydratableObject.Rehydrate),
            $"Domain.Common.ValueObjectFactory<{className}>",
            null,
            isSingleValueObject
                ? $"return (property, container) =>\n{{\nvar parts = RehydrateToList(property, true);\nreturn new {className}(parts[0]);\n}};"
                : $"return (property, container) =>\n{{\nvar parts = RehydrateToList(property, false);\nreturn new {className}(parts[0], parts[1], parts[2]);\n}};");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddCreateMethodToValueObject(Document document,
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
        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword },
            DomainDrivenDesignAnalyzer.ClassFactoryMethodName,
            $"Result<{className}, Error>",
            isSingleValueObject
                ? new Dictionary<string, string>
                {
                    { "value", nameof(String).ToLowerInvariant() }
                }
                : new Dictionary<string, string>
                {
                    { "value1", nameof(String).ToLowerInvariant() },
                    { "value2", nameof(String).ToLowerInvariant() },
                    { "value3", nameof(String).ToLowerInvariant() }
                },
            isSingleValueObject
                ? $"if (value.IsNotValuedParameter(nameof(value), out var error))\n{{\nreturn error;\n}}\n\nreturn new {className}(value);"
                : $"if (value1.IsNotValuedParameter(nameof(value1), out var error1))\n{{\nreturn error1;\n}}\n\nreturn new {className}(value1, value2, value3);");
        var modifiedClassDeclaration = classDeclarationSyntax.InsertMember(0, newMethod);
        if (!isSingleValueObject)
        {
            var newConstructor = GenerateConstructor(
                new[] { SyntaxKind.PrivateKeyword },
                className,
                new Dictionary<string, string>
                {
                    { "value1", nameof(String).ToLowerInvariant() },
                    { "value2", nameof(String).ToLowerInvariant() },
                    { "value3", nameof(String).ToLowerInvariant() }
                },
                null);
            modifiedClassDeclaration = modifiedClassDeclaration.InsertMember(1, newConstructor);
        }

        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddDehydrateMethodToAggregateRoot(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.OverrideKeyword },
            nameof(IDehydratableEntity.Dehydrate),
            "Dictionary<string, object?>",
            null,
            "var properties = base.Dehydrate();\nreturn properties;");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddDehydrateMethodToEntity(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var newMethod = GenerateMethod(
            new[] { SyntaxKind.PublicKeyword, SyntaxKind.OverrideKeyword },
            nameof(IDehydratableEntity.Dehydrate),
            "Dictionary<string, object?>",
            null,
            "var properties = base.Dehydrate();\nproperties.Add(nameof(RootId), RootId);\nreturn properties;");
        var modifiedClassDeclaration = classDeclarationSyntax.AddMembers(newMethod);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddEntityValueAttribute(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var className = classDeclarationSyntax.Identifier.Text;
        var newAttribute = GenerateAttribute(nameof(EntityNameAttribute), $"\"{className}\"");
        var modifiedClassDeclaration = classDeclarationSyntax.AddAttributeLists(newAttribute);
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static AttributeListSyntax GenerateAttribute(string name, string? firstArgumentExpression)
    {
        var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(name));
        if (firstArgumentExpression.Exists())
        {
            attribute = attribute.WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(
                new List<AttributeArgumentSyntax>
                    { SyntaxFactory.AttributeArgument(SyntaxFactory.ParseExpression(firstArgumentExpression)) })));
        }

        return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
    }

    private static MethodDeclarationSyntax GenerateMethod(SyntaxKind[] accessibility, string methodName,
        string? returnType, Dictionary<string, string>? parameters, string? bodyWithoutBlock)
    {
        var methodModifiers = accessibility.HasAny()
            ? SyntaxFactory.TokenList(accessibility.Select(SyntaxFactory.Token))
            : SyntaxFactory.TokenList();
        var methodParameters = parameters.Exists()
            ? SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Select(parameter =>
            {
                var syntax = SyntaxFactory
                    .Parameter(SyntaxFactory.Identifier(parameter.Key))
                    .WithType(SyntaxFactory.ParseTypeName(parameter.Value));
                return syntax;
            })))
            : SyntaxFactory.ParameterList();
        var bodySyntax = bodyWithoutBlock.Exists()
            ? (BlockSyntax)SyntaxFactory.ParseStatement($"{{\n{bodyWithoutBlock}\n}}")
            : (BlockSyntax)SyntaxFactory.ParseStatement("{\n}");

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                methodModifiers,
                returnType.Exists()
                    ? SyntaxFactory.ParseTypeName(returnType)
                    : SyntaxFactory.ParseTypeName(SyntaxFacts.GetText(SyntaxKind.VoidKeyword)),
                null!,
                SyntaxFactory.Identifier(methodName),
                null!,
                methodParameters,
                SyntaxFactory.List<TypeParameterConstraintClauseSyntax>(),
                bodySyntax,
                null!)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }

    private static ConstructorDeclarationSyntax GenerateConstructor(SyntaxKind[] accessibility, string methodName,
        Dictionary<string, string>? parameters, string? bodyWithoutBlock)
    {
        var methodModifiers = accessibility.HasAny()
            ? SyntaxFactory.TokenList(accessibility.Select(SyntaxFactory.Token))
            : SyntaxFactory.TokenList();
        var methodParameters = parameters.Exists()
            ? SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Select(parameter =>
            {
                var syntax = SyntaxFactory
                    .Parameter(SyntaxFactory.Identifier(parameter.Key))
                    .WithType(SyntaxFactory.ParseTypeName(parameter.Value));
                return syntax;
            })))
            : SyntaxFactory.ParameterList();
        var bodySyntax = bodyWithoutBlock.Exists()
            ? (BlockSyntax)SyntaxFactory.ParseStatement($"{{\n{bodyWithoutBlock}\n}}")
            : (BlockSyntax)SyntaxFactory.ParseStatement("{\n}");

        return SyntaxFactory.ConstructorDeclaration(
                SyntaxFactory.List<AttributeListSyntax>(),
                methodModifiers,
                SyntaxFactory.Identifier(methodName),
                methodParameters,
                null,
                bodySyntax)
            .WithAdditionalAnnotations(Formatter.Annotation);
    }
}