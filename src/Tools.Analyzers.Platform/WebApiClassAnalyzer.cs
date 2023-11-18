using System.Collections.Immutable;
using System.Text;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Tools.Analyzers.Platform.Extensions;

// ReSharper disable InvalidXmlDocComment

namespace Tools.Analyzers.Platform;

/// <summary>
///     An analyzer to ensure that WebAPI classes are configured correctly.
///     SAS010: Warning: Methods that are public, should return a <see cref="Task{T}" /> or just any T, where T is either:
///     <see cref="ApiEmptyResult" /> or <see cref="ApiResult{TResource,TResponse}" /> or
///     <see cref="ApiPostResult{TResource, TResponse}" />
///     SAS011: Warning: These methods must have at least one parameter, and first parameter must be
///     <see cref="IWebRequest{TResponse}" />, where
///     TResponse is same type as in the return value.
///     SAS012: Warning: The second parameter can only be a <see cref="CancellationToken" />
///     SAS013: Warning: These methods must be decorated with a <see cref="RouteAttribute" />
///     SAS014: Warning: The route (of all these methods in this class) should start with the same path
///     SAS015: Warning: There should be no methods in this class with the same <see cref="IWebRequest{TResponse}" />
///     SAS016: Warning: This service operation should return an appropriate Result type for the operation
///     SAS017: Warning: The request type should be declared with a <see cref="RouteAttribute" /> on it
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class WebApiClassAnalyzer : DiagnosticAnalyzer
{
    internal static readonly Dictionary<Infrastructure.Web.Api.Interfaces.ServiceOperation, List<Type>>
        AllowableOperationReturnTypes =
            new()
            {
                {
                    Infrastructure.Web.Api.Interfaces.ServiceOperation.Post,
                    new List<Type> { typeof(ApiEmptyResult), typeof(ApiPostResult<,>) }
                },
                {
                    Infrastructure.Web.Api.Interfaces.ServiceOperation.Get,
                    new List<Type> { typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiGetResult<,>) }
                },
                {
                    Infrastructure.Web.Api.Interfaces.ServiceOperation.Search,
                    new List<Type>
                    {
                        typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiGetResult<,>),
                        typeof(ApiSearchResult<,>)
                    }
                },
                {
                    Infrastructure.Web.Api.Interfaces.ServiceOperation.PutPatch,
                    new List<Type> { typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiPutPatchResult<,>) }
                },
                {
                    Infrastructure.Web.Api.Interfaces.ServiceOperation.Delete,
                    new List<Type> { typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiDeleteResult) }
                }
            };

    internal static readonly Type[] AllowableReturnTypes =
    {
        typeof(ApiEmptyResult),
        typeof(ApiResult<,>),
        typeof(ApiPostResult<,>),
        typeof(ApiGetResult<,>),
        typeof(ApiSearchResult<,>),
        typeof(ApiPutPatchResult<,>),
        typeof(ApiDeleteResult)
    };

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

    internal static readonly DiagnosticDescriptor Sas017 = "SAS017".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAS017Title), nameof(Resources.SAS017Description),
        nameof(Resources.SAS017MessageFormat));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Sas010, Sas011, Sas012, Sas013, Sas014, Sas015, Sas016, Sas017);

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

        if (context.IsExcludedInNamespace(classDeclarationSyntax, AnalyzerConstants.PlatformNamespaces))
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

            if (ParametersAreInCorrect(context, methodDeclarationSyntax, out var requestTypeSyntax))
            {
                continue;
            }

            if (AttributeIsNotPresent(context, methodDeclarationSyntax, requestTypeSyntax!, out var attribute))
            {
                continue;
            }

            var requestType = requestTypeSyntax!.GetBaseOfType<IWebRequest>(context);
            operations.Add(methodDeclarationSyntax, new ServiceOperation(requestType!));

            var operation =
                (Infrastructure.Web.Api.Interfaces.ServiceOperation)attribute!.ConstructorArguments[1].Value!;
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
        MethodDeclarationSyntax methodDeclarationSyntax, Infrastructure.Web.Api.Interfaces.ServiceOperation operation,
        ITypeSymbol returnType)
    {
        var allowedReturnTypes = AllowableOperationReturnTypes[operation];

        if (MatchesAllowedTypes(context, returnType, allowedReturnTypes.ToArray()))
        {
            return false;
        }

        context.ReportDiagnostic(Sas016, methodDeclarationSyntax, operation,
            allowedReturnTypes.ToArray().Stringify());

        return true;
    }

    private static bool ReturnTypeIsNotCorrect(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, out ITypeSymbol? returnType)
    {
        returnType = null;
        var symbol = context.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
        if (symbol is null)
        {
            return true;
        }

        returnType = symbol.ReturnType;
        if (returnType.IsVoid(context))
        {
            context.ReportDiagnostic(Sas010, methodDeclarationSyntax, AllowableReturnTypes.Stringify());
            return true;
        }

        if (returnType.IsVoidTask(context))
        {
            context.ReportDiagnostic(Sas010, methodDeclarationSyntax, AllowableReturnTypes.Stringify());
            return true;
        }

        if (!MatchesAllowedTypes(context, returnType, AllowableReturnTypes))
        {
            context.ReportDiagnostic(Sas010, methodDeclarationSyntax, AllowableReturnTypes.Stringify());
            return true;
        }

        return false;
    }

    private static bool AttributeIsNotPresent(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, ParameterSyntax requestTypeSyntax,
        out AttributeData? attribute)
    {
        var requestTypeSymbol = context.SemanticModel.GetSymbolInfo(requestTypeSyntax.Type!).Symbol!;
        attribute = requestTypeSymbol.GetAttributeOfType<RouteAttribute>(context);
        if (attribute is null)
        {
            context.ReportDiagnostic(Sas013, methodDeclarationSyntax);
            if (requestTypeSyntax.Type is IdentifierNameSyntax nameSyntax)
            {
                context.ReportDiagnostic(Sas017, nameSyntax);
            }

            return true;
        }

        return false;
    }

    private static bool ParametersAreInCorrect(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, out ParameterSyntax? requestTypeSyntax)
    {
        requestTypeSyntax = null;
        var parameters = methodDeclarationSyntax.ParameterList.Parameters;
        if (parameters.Count is < 1 or > 2)
        {
            context.ReportDiagnostic(Sas011, methodDeclarationSyntax);
            return true;
        }

        var firstParam = parameters.First();
        var requestType = firstParam.GetBaseOfType<IWebRequest>(context);
        if (requestType is null)
        {
            context.ReportDiagnostic(Sas011, methodDeclarationSyntax);
            return true;
        }

        requestTypeSyntax = firstParam;

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

    /// <summary>
    ///     Determines whether the <see cref="returnType" /> is the same as one of the <see cref="AllowableReturnTypes" />
    ///     , or one of the <see cref="AllowableReturnTypes" /> as a <see cref="Task{T}" />
    /// </summary>
    private static bool MatchesAllowedTypes(SyntaxNodeAnalysisContext context, ITypeSymbol returnType,
        Type[] allowableReturnTypes)
    {
        var nakedType = returnType;
        if (IsGenericTask())
        {
            //Task<T>
            if (returnType is not INamedTypeSymbol namedTypeSymbol)
            {
                return false;
            }

            nakedType = namedTypeSymbol.TypeArguments.First();
        }

        foreach (var allowedType in allowableReturnTypes)
        {
            var allowedTypeNaked = context.Compilation.GetTypeByMetadataName(allowedType.FullName!)!;
            if (nakedType.OriginalDefinition.IsOfType(allowedTypeNaked))
            {
                return true;
            }

            var taskType = context.Compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            var returnTypeTasked =
                taskType.Construct(context.Compilation.GetTypeByMetadataName(allowedType.FullName!)!);
            var allowedTypeTasked =
                taskType.Construct(context.Compilation.GetTypeByMetadataName(allowedType.FullName!)!);
            if (returnTypeTasked.OriginalDefinition.IsOfType(allowedTypeTasked))
            {
                return true;
            }
        }

        return false;

        bool IsGenericTask()
        {
            var genericTaskSymbol = context.Compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!;
            return returnType.OriginalDefinition.IsOfType(genericTaskSymbol);
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
                RouteSegments = routePath.Split("/", StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}

internal static class TypeExtensions
{
    public static string Stringify(this Type[] allowableReturnTypes)
    {
        var taskOfList = new StringBuilder();
        var nakedList = new StringBuilder();

        var stringifiedTypes = allowableReturnTypes.Select(Stringify).ToList();
        nakedList.AppendJoin(", ", stringifiedTypes);
        taskOfList.AppendJoin(", ", stringifiedTypes.Select(s => $"Task<{s}>"));

        return $"{taskOfList}, or {nakedList}";
    }

    private static string Stringify(this Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var arguments = type.GetGenericArguments();
        var builder = new StringBuilder();

        builder.Append(type.Name.Substring(0, type.Name.LastIndexOf('`')));
        builder.Append("<");
        builder.AppendJoin(", ", arguments.Select(arg => arg.Name));
        builder.Append(">");

        return builder.ToString();
    }
}