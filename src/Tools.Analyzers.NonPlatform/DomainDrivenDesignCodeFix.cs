using System.Collections.Immutable;
using System.Composition;
using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using QueryAny;
using SyntaxExtensions = Tools.Analyzers.Common.Extensions.SyntaxExtensions;
using Task = System.Threading.Tasks.Task;

namespace Tools.Analyzers.NonPlatform;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DomainDrivenDesignCodeFix))]
public class DomainDrivenDesignCodeFix : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(
            DomainDrivenDesignAnalyzer.Rule010.Id,
            DomainDrivenDesignAnalyzer.Rule014.Id,
            DomainDrivenDesignAnalyzer.Rule015.Id,
            DomainDrivenDesignAnalyzer.Rule016.Id,
            DomainDrivenDesignAnalyzer.Rule018.Id,
            DomainDrivenDesignAnalyzer.Rule020.Id,
            DomainDrivenDesignAnalyzer.Rule023.Id,
            DomainDrivenDesignAnalyzer.Rule024.Id,
            DomainDrivenDesignAnalyzer.Rule025.Id,
            DomainDrivenDesignAnalyzer.Rule027.Id,
            DomainDrivenDesignAnalyzer.Rule030.Id,
            DomainDrivenDesignAnalyzer.Rule033.Id,
            DomainDrivenDesignAnalyzer.Rule035.Id,
            DomainDrivenDesignAnalyzer.Rule036.Id,
            DomainDrivenDesignAnalyzer.Rule041.Id
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

        if (syntax is MethodDeclarationSyntax methodDeclarationSyntax)
        {
            FixMethod(context, methodDeclarationSyntax);
            return;
        }

        if (syntax is ClassDeclarationSyntax classDeclarationSyntax)
        {
            FixClass(context, classDeclarationSyntax);
        }
    }

    private static void FixClass(CodeFixContext context, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var diagnostics = context.Diagnostics;
        var diagnostic = diagnostics.First();

        if (diagnostics.Any(d => d.Id == DomainDrivenDesignAnalyzer.Rule014.Id))
        {
            var title = Resources.CodeFix_Title_AddRehydrateMethod;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddRehydrateMethodToAggregateRoot(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d => d.Id == DomainDrivenDesignAnalyzer.Rule023.Id))
        {
            var title = Resources.CodeFix_Title_AddRehydrateMethod;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddRehydrateMethodToEntity(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d => d.Id == DomainDrivenDesignAnalyzer.Rule033.Id))
        {
            var title = Resources.CodeFix_Title_AddRehydrateMethod;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddRehydrateMethodToValueObject(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule015.Id))
        {
            var title = Resources.CodeFix_Title_AddDehydrateMethodToEntity;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddDehydrateMethodToAggregateRoot(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }
        
        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule018.Id
                || d.Id == DomainDrivenDesignAnalyzer.Rule027.Id
                || d.Id == DomainDrivenDesignAnalyzer.Rule036.Id
                || d.Id == DomainDrivenDesignAnalyzer.Rule041.Id))
        {
            var title = Resources.CodeFix_Title_AddSealedToClass;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddSealedToClass(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule024.Id))
        {
            var title = Resources.CodeFix_Title_AddDehydrateMethodToEntity;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddDehydrateMethodToEntity(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule010.Id))
        {
            var title = Resources.CodeFix_Title_AddClassFactoryMethodToAggregate;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddCreateMethodToAggregateRoot(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule020.Id))
        {
            var title = Resources.CodeFix_Title_AddClassFactoryMethodToEntity;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddCreateMethodToEntity(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule030.Id))
        {
            var title = Resources.CodeFix_Title_AddClassFactoryMethodToValueObject;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddCreateMethodToValueObject(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule016.Id || d.Id == DomainDrivenDesignAnalyzer.Rule025.Id))
        {
            var title = Resources.CodeFix_Title_AddEntityValueAttributeToEntiyOrAggregate;
            context.RegisterCodeFix(
                CodeAction.Create(title,
                    token => AddEntityValueAttribute(context.Document, classDeclarationSyntax, token),
                    title),
                diagnostic);
        }
    }

    private static void FixMethod(CodeFixContext context, MethodDeclarationSyntax methodDeclarationSyntax)
    {
        var diagnostics = context.Diagnostics;
        var diagnostic = diagnostics.First();

        if (diagnostics.Any(d =>
                d.Id == DomainDrivenDesignAnalyzer.Rule035.Id))
        {
            var title1 = Resources.CodeFix_Title_AddSkipImmutabilityCheckAttributeToValueObjectMethod;
            context.RegisterCodeFix(
                CodeAction.Create(title1,
                    token => AddSkipImmutabilityCheckAttribute(context.Document, methodDeclarationSyntax, token),
                    title1),
                diagnostic);
            var title2 = Resources.CodeFix_Title_ChangeValueObjectMethodReturnType;
            context.RegisterCodeFix(
                CodeAction.Create(title2,
                    token => ChangeImmutableReturnType(context.Document, methodDeclarationSyntax, token),
                    title2),
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
            [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
            nameof(IRehydratableObject.Rehydrate),
            $"Domain.Interfaces.AggregateRootFactory<{className}>",
            null,
            isDehydratable.IsDehydratable
                ? $"return (identifier, container, properties) => new {className}(identifier, container, properties);"
                : $"return (identifier, container, properties) => new {className}(container.GetRequiredService<{nameof(IRecorder)}>(),container.GetRequiredService<{nameof(IIdentifierFactory)}>(), identifier);");
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
            [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
            DomainDrivenDesignAnalyzer.ClassFactoryMethodName,
            $"{nameof(Result)}<{className}, {nameof(Error)}>",
            new Dictionary<string, string>
            {
                { "recorder", nameof(IRecorder) },
                { "idFactory", nameof(IIdentifierFactory) },
                { "organizationId", nameof(Identifier) }
            },
            $"var root = new {className}(recorder, idFactory);\nroot.RaiseCreateEvent(Created.Create(root.Id, organizationId));\nreturn root;");
        var modifiedClassDeclaration = SyntaxExtensions.InsertMember(classDeclarationSyntax, 0, newMethod);
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
            [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
            nameof(IRehydratableObject.Rehydrate),
            $"Domain.Interfaces.EntityFactory<{className}>",
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
            [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
            DomainDrivenDesignAnalyzer.ClassFactoryMethodName,
            $"{nameof(Result)}<{className}, {nameof(Error)}>",
            new Dictionary<string, string>
            {
                { "recorder", nameof(IRecorder) },
                { "idFactory", nameof(IIdentifierFactory) },
                { "rootEventHandler", nameof(RootEventHandler) }
            },
            $"return new {className}(recorder, idFactory, rootEventHandler);");
        var modifiedClassDeclaration = SyntaxExtensions.InsertMember(classDeclarationSyntax, 0, newMethod);
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
            [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
            nameof(IRehydratableObject.Rehydrate),
            $"Domain.Interfaces.ValueObjectFactory<{className}>",
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
            [SyntaxKind.PublicKeyword, SyntaxKind.StaticKeyword],
            DomainDrivenDesignAnalyzer.ClassFactoryMethodName,
            $"{nameof(Result)}<{className}, {nameof(Error)}>",
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
        var modifiedClassDeclaration = SyntaxExtensions.InsertMember(classDeclarationSyntax, 0, newMethod);
        if (!isSingleValueObject)
        {
            var newConstructor = GenerateConstructor(
                [SyntaxKind.PrivateKeyword],
                className,
                new Dictionary<string, string>
                {
                    { "value1", nameof(String).ToLowerInvariant() },
                    { "value2", nameof(String).ToLowerInvariant() },
                    { "value3", nameof(String).ToLowerInvariant() }
                },
                null);
            modifiedClassDeclaration = SyntaxExtensions.InsertMember(modifiedClassDeclaration, 1, newConstructor);
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
            [SyntaxKind.PublicKeyword, SyntaxKind.OverrideKeyword],
            nameof(IDehydratableEntity.Dehydrate),
            $"{nameof(HydrationProperties)}",
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
            [SyntaxKind.PublicKeyword, SyntaxKind.OverrideKeyword],
            nameof(IDehydratableEntity.Dehydrate),
            $"{nameof(HydrationProperties)}",
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

    private static async Task<Solution> AddSkipImmutabilityCheckAttribute(Document document,
        MethodDeclarationSyntax methodDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var newAttribute = GenerateAttribute(typeof(SkipImmutabilityCheckAttribute).FullName!);
        var modifiedMethodDeclaration = methodDeclarationSyntax.AddAttributeLists(newAttribute);
        var newRoot = root.ReplaceNode(methodDeclarationSyntax, modifiedMethodDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> ChangeImmutableReturnType(Document document,
        MethodDeclarationSyntax methodDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var className = (ClassDeclarationSyntax)methodDeclarationSyntax.Parent!;
        var body = methodDeclarationSyntax.Body!.Statements;
        if (body.HasNone())
        {
            body = new SyntaxList<StatementSyntax>(
                SyntaxFactory.ParseStatement($"return {DomainDrivenDesignAnalyzer.ClassFactoryMethodName}();"));
        }

        var newRoot = root.ReplaceNode(methodDeclarationSyntax,
            methodDeclarationSyntax.WithReturnType(SyntaxFactory.GenericName(nameof(Result))
                    .AddTypeArgumentListArguments(SyntaxFactory.ParseTypeName(className.Identifier.Text),
                        SyntaxFactory.ParseTypeName(nameof(Error))))
                .WithBody(SyntaxFactory.Block(body)));

        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static async Task<Solution> AddSealedToClass(Document document,
        ClassDeclarationSyntax classDeclarationSyntax, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken);
        if (root.NotExists())
        {
            return document.Project.Solution;
        }

        var modifiedClassDeclaration = classDeclarationSyntax.AddModifiers(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
        var newRoot = root.ReplaceNode(classDeclarationSyntax, modifiedClassDeclaration);
        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }

    private static AttributeListSyntax GenerateAttribute(string name, string? firstArgumentExpression = null)
    {
        var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(name));
        if (firstArgumentExpression.HasValue())
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