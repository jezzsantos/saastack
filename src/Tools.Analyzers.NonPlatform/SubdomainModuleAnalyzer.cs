using System.Collections.Immutable;
using Common.Extensions;
using Domain.Interfaces.Entities;
using Infrastructure.Web.Hosting.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tools.Analyzers.Common;
using Tools.Analyzers.Common.Extensions;
using Tools.Analyzers.NonPlatform.Extensions;

namespace Tools.Analyzers.NonPlatform;

/// <summary>
///     An analyzer to correct the implementation of WebAPI classes, and their requests and responses.
///     SAASHST10: Error: Aggregate root or Entity should register an identity prefix
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SubdomainModuleAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Rule010 = "SAASHST10".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.Host, nameof(Resources.SAASHST010Title), nameof(Resources.SAASHST010Description),
        nameof(Resources.SAASHST010MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule010);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeSubdomainModule, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeSubdomainModule(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = context.Node;
        if (methodSyntax is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (context.IsExcludedInNamespace(classDeclarationSyntax, AnalyzerConstants.PlatformNamespaces))
        {
            return;
        }

        if (classDeclarationSyntax.IsNotType<ISubdomainModule>(context))
        {
            return;
        }

        var domainAssemblyProperty = classDeclarationSyntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(method => method.Identifier.Text == nameof(ISubdomainModule.DomainAssembly));
        if (domainAssemblyProperty is null)
        {
            return;
        }

        var assemblySymbol = domainAssemblyProperty.GetAssemblyFromProperty(context);
        if (assemblySymbol is null)
        {
            return;
        }

        var allEntities = assemblySymbol.GetAllEntityTypes(context);
        if (allEntities.HasNone())
        {
            return;
        }

        var entityPrefixesProperty = classDeclarationSyntax.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(method => method.Identifier.Text == nameof(ISubdomainModule.EntityPrefixes));
        if (entityPrefixesProperty is null)
        {
            return;
        }

        var allRegisteredPrefixes = entityPrefixesProperty.GetPrefixesFromProperty(context);
        if (allRegisteredPrefixes.HasNone())
        {
            foreach (var entity in allEntities)
            {
                context.ReportDiagnostic(Rule010, entityPrefixesProperty, entity.Name);
            }

            return;
        }

        foreach (var entity in allEntities)
        {
            if (!allRegisteredPrefixes.Contains(entity))
            {
                context.ReportDiagnostic(Rule010, entityPrefixesProperty, entity.Name);
            }
        }
    }
}

internal static class SubdomainModuleAnalyzerExtensions
{
    public static List<INamedTypeSymbol> GetAllEntityTypes(this IAssemblySymbol assembly,
        SyntaxNodeAnalysisContext context)
    {
        var aggregate = context.Compilation.GetTypeByMetadataName(typeof(IAggregateRoot).FullName!)!;
        var entity = context.Compilation.GetTypeByMetadataName(typeof(IEntity).FullName!)!;

        return assembly.GlobalNamespace.GetMembers()
            .SelectMany(ns => ns.GetMembers())
            .OfType<INamedTypeSymbol>()
            .Where(type => type.AllInterfaces.Contains(aggregate) || type.AllInterfaces.Contains(entity))
            .ToList();
    }

    public static IAssemblySymbol? GetAssemblyFromProperty(this PropertyDeclarationSyntax property,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(property);
        if (symbol is null)
        {
            return null;
        }

        var getter = symbol.GetMethod;
        if (getter is null)
        {
            return null;
        }

        var body = property.GetGetterBody();
        if (body is null)
        {
            return null;
        }

        var someType = body.DescendantNodes()
            .OfType<TypeOfExpressionSyntax>()
            .Select(expr => expr.Type)
            .FirstOrDefault();
        if (someType is null)
        {
            return null;
        }

        return context.SemanticModel.GetSymbolInfo(someType)
            .Symbol?.ContainingAssembly;
    }

    public static List<INamedTypeSymbol> GetPrefixesFromProperty(this PropertyDeclarationSyntax property,
        SyntaxNodeAnalysisContext context)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(property);
        if (symbol is null)
        {
            return new List<INamedTypeSymbol>();
        }

        var getter = symbol.GetMethod;
        if (getter is null)
        {
            return new List<INamedTypeSymbol>();
        }

        var body = property.GetGetterBody();
        if (body is null)
        {
            return new List<INamedTypeSymbol>();
        }

        var someTypes = body.DescendantNodes()
            .OfType<TypeOfExpressionSyntax>()
            .Select(expr => expr.Type)
            .ToList();
        if (someTypes.HasNone())
        {
            return new List<INamedTypeSymbol>();
        }

        return someTypes
            .Select(someType => context.SemanticModel.GetSymbolInfo(someType).Symbol)
            .OfType<INamedTypeSymbol>()
            .ToList();
    }

    private static ExpressionSyntax? GetGetterBody(this PropertyDeclarationSyntax property)
    {
        var body = property.ExpressionBody;
        if (body.Exists())
        {
            return body.Expression;
        }

        return null;
    }
}