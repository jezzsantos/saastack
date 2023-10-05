using System.Collections.Immutable;
using Common.Extensions;
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tools.Analyzers.Core.Extensions;

// ReSharper disable InvalidXmlDocComment

namespace Tools.Analyzers.Core;

/// <summary>
///     An analyzer to ensure that WebAPI classes are configured correctly.
///     SAS010. Warning: Methods that are public, should return a <see cref="Task{T}" /> or just any T, where T is either:
///     <see cref="ApiEmptyResult" /> or <see cref="ApiResult{TResource, TResponse}" /> or
///     <see cref="ApiPostResult{TResource, TResponse}" />
///     SAS011. Warning: These methods must have at least one parameter, and first parameter must be
///     <see cref="IWebRequest{TResponse}" />, where
///     TResponse is same type as in the return value.
///     SAS012. Warning: The second parameter can only be a <see cref="CancellationToken" />
///     SAS013. Warning: These methods must be decorated with a <see cref="WebApiRouteAttribute" />
///     SAS014. Warning: The route (of all these methods in this class) should start with the same path
///     SAS015. Warning: There should be no methods in this class with the same <see cref="IWebRequest{TResponse}" />
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WebApiClassAnalyzer : DiagnosticAnalyzer
{
    internal static readonly DiagnosticDescriptor Sas010 = "SAS010".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS010Title), nameof(Resources.SAS010Description),
        nameof(Resources.SAS010MessageFormat));

    internal static readonly DiagnosticDescriptor Sas011 = "SAS011".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS011Title), nameof(Resources.SAS011Description),
        nameof(Resources.SAS011MessageFormat));

    internal static readonly DiagnosticDescriptor Sas012 = "SAS012".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS012Title), nameof(Resources.SAS012Description),
        nameof(Resources.SAS012MessageFormat));

    internal static readonly DiagnosticDescriptor Sas013 = "SAS013".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS013Title), nameof(Resources.SAS013Description),
        nameof(Resources.SAS013MessageFormat));

    internal static readonly DiagnosticDescriptor Sas014 = "SAS014".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS014Title), nameof(Resources.SAS014Description),
        nameof(Resources.SAS014MessageFormat));

    internal static readonly DiagnosticDescriptor Sas015 = "SAS015".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS015Title), nameof(Resources.SAS015Description),
        nameof(Resources.SAS015MessageFormat));

    internal static readonly DiagnosticDescriptor Sas016 = "SAS016".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS016Title), nameof(Resources.SAS016Description),
        nameof(Resources.SAS016MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Sas010, Sas011, Sas012, Sas013, Sas014, Sas015, Sas016);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var methodSyntax = context.Node;
        if (methodSyntax is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (context.IsExcludedInNamespace(AnalyzerConstants.CommonNamespaces))
        {
            return;
        }

        if (classDeclarationSyntax.IsNotType<IWebApiService>(context))
        {
            return;
        }

        var allMethods = classDeclarationSyntax.Members.Where(member => member is MethodDeclarationSyntax)
            .Cast<MethodDeclarationSyntax>();
        var operations = new Dictionary<MethodDeclarationSyntax, ServiceOperation>();
        foreach (var methodDeclarationSyntax in allMethods)
        {
            if (methodDeclarationSyntax.IsNotPublicInstanceMethod())
            {
                continue;
            }

            if (ReturnTypeIsNotCorrect(context, methodDeclarationSyntax, out var returnType))
            {
                continue;
            }

            if (ParametersAreInCorrect(context, methodDeclarationSyntax, out var requestType))
            {
                continue;
            }

            operations.Add(methodDeclarationSyntax, new ServiceOperation(requestType!));

            if (AttributeIsNotPresent(context, methodDeclarationSyntax, out var attribute))
            {
                continue;
            }

            var operation = (WebApiOperation)attribute!.ConstructorArguments[1].Value!;
            if (OperationAndReturnsTypeDontMatch(context, methodDeclarationSyntax, operation, returnType!))
            {
                continue;
            }

            var routePath = attribute.ConstructorArguments[0]
                .Value?.ToString();
            operations[methodDeclarationSyntax]
                .SetRouteSegments(routePath);
        }

        RoutesHaveSamePrimaryResource(context, operations);
        RequestTypesAreNotDuplicated(context, operations);
    }

    private static bool OperationAndReturnsTypeDontMatch(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, WebApiOperation operation, ITypeSymbol returnType)
    {
        if (operation != WebApiOperation.Post)
        {
            return false;
        }

        var type = typeof(ApiPostResult<,>);
        var postResult = context.Compilation.GetTypeByMetadataName(type.FullName!)!;
        if (!SymbolEqualityComparer.Default.Equals(returnType.OriginalDefinition, postResult))
        {
            var typeName =
                "ApiPostResult<TResource, TResponse>"; // HACK: dont know how to get this same string from the type itself 
            context.ReportDiagnostic(Sas016, methodDeclarationSyntax, operation, typeName);
            return true;
        }

        return false;
    }

    private static bool ReturnTypeIsNotCorrect(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, out ITypeSymbol? returnType)
    {
        if (methodDeclarationSyntax.IsReturnTypeNotMatching(context, out returnType, typeof(ApiEmptyResult),
                typeof(ApiResult<,>), typeof(ApiPostResult<,>)))
        {
            context.ReportDiagnostic(Sas010, methodDeclarationSyntax);
            return true;
        }

        return false;
    }

    private static bool AttributeIsNotPresent(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, out AttributeData? attribute)
    {
        attribute = null;
        var attributes = methodDeclarationSyntax.AttributeLists;
        if (attributes.Count == 0)
        {
            context.ReportDiagnostic(Sas013, methodDeclarationSyntax);
            return true;
        }

        attribute = methodDeclarationSyntax.GetAttributeOfType<WebApiRouteAttribute>(context);
        if (attribute is null)
        {
            context.ReportDiagnostic(Sas013, methodDeclarationSyntax);
            return true;
        }

        return false;
    }

    private static bool ParametersAreInCorrect(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, out ITypeSymbol? requestType)
    {
        requestType = null;
        var parameters = methodDeclarationSyntax.ParameterList.Parameters;
        if (parameters.Count is < 1 or > 2)
        {
            context.ReportDiagnostic(Sas011, methodDeclarationSyntax);
            return true;
        }

        var firstParam = parameters.First();
        requestType = firstParam.GetBaseOfType<IWebRequest>(context);
        if (requestType is null)
        {
            context.ReportDiagnostic(Sas011, methodDeclarationSyntax);
            return true;
        }

        if (parameters.Count == 2)
        {
            var secondParam = parameters[1];
            if (secondParam.IsNotType<CancellationToken>(context))
            {
                context.ReportDiagnostic(Sas012, methodDeclarationSyntax);
                return true;
            }
        }

        return false;
    }

    private static void RequestTypesAreNotDuplicated(SyntaxNodeAnalysisContext context,
        Dictionary<MethodDeclarationSyntax, ServiceOperation> operations)
    {
        var duplicateRequestTypes = operations.GroupBy(ops => ops.Value.RequestType.ToDisplayString())
            .Where(grp => grp.Count() > 1);
        foreach (var duplicateGroup in duplicateRequestTypes)
        {
            foreach (var entry in duplicateGroup)
            {
                context.ReportDiagnostic(Sas015, entry.Key);
            }
        }
    }

    private static void RoutesHaveSamePrimaryResource(SyntaxNodeAnalysisContext context,
        Dictionary<MethodDeclarationSyntax, ServiceOperation> operations)
    {
        var primaryResource = string.Empty;
        foreach (var operation in operations.Where(ops => ops.Value.RouteSegments.Any()))
        {
            if (primaryResource.HasNoValue())
            {
                primaryResource = operation.Value.RouteSegments.First();
                continue;
            }

            if (operation.Value.RouteSegments.First() != primaryResource)
            {
                context.ReportDiagnostic(Sas014, operation.Key);
            }
        }
    }

    private class ServiceOperation
    {
        public ServiceOperation(ITypeSymbol requestType)
        {
            RequestType = requestType;
        }

        public ITypeSymbol RequestType { get; }

        public IEnumerable<string> RouteSegments { get; private set; } = Enumerable.Empty<string>();

        public void SetRouteSegments(string? routePath)
        {
            if (routePath.HasValue())
            {
                RouteSegments = routePath!.Split("/", StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}