using System.Collections.Immutable;
using System.Text;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
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
///     WebApiServices:
///     SAASWEB10: Warning: Methods that are public, should return a <see cref="Task{T}" /> or just any T, where T is
///     either:
///     <see cref="ApiEmptyResult" /> or <see cref="ApiResult{TResource,TResponse}" /> or
///     <see cref="ApiPostResult{TResource, TResponse}" />
///     SAASWEB11: Warning: These methods must have at least one parameter, and first parameter must be
///     <see cref="IWebRequest{TResponse}" />, where
///     TResponse is same type as in the return value.
///     SAASWEB12: Warning: The second parameter can only be a <see cref="CancellationToken" />
///     SAASWEB13: Warning: These methods must be decorated with a <see cref="RouteAttribute" />
///     SAASWEB14: Warning: The route (of all service operations in this class) should start with the same path
///     SAASWEB15: Warning: There should be no service operations in this class with the same
///     <see cref="IWebRequest{TResponse}" />
///     SAASWEB16: Warning: This service operation should return an appropriate Result type for the operation
///     SAASWEB17: Warning: The request type should be declared with a <see cref="RouteAttribute" /> on it
///     SAASWEB18: Error: There should not be an <see cref="AuthorizeAttribute" /> if the <see cref="RouteAttribute" />
///     declares <see cref="AccessType.Anonymous" /> access
///     SAASWEB19: Warning: There should be a <see cref="AuthorizeAttribute" /> if the <see cref="RouteAttribute" />
///     declares anything other than <see cref="AccessType.Anonymous" /> access
///     SAASWEB20: Warning: There should be no service operations in this class with the same route and method
///     Requests:
///     SAASWEB30: Error: Request must be public
///     SAASWEB31: Error: Request must be named with "Request" suffix
///     SAASWEB32: Error: Request must be in namespace "Infrastructure.Web.Api.Operations.Shared"
///     SAASWEB33: Error: Request must have a <see cref="RouteAttribute" />
///     SAASWEB34: Error: Request must have a parameterless constructor
///     SAASWEB35: Error: Properties must have public getters and setters
///     SAASWEB36: Error: Properties should be nullable not Optional{T} for interoperability
///     SAASWEB37: Error: Properties should NOT use required modifier
///     SAASWEB38: Error: Properties for GET/DELETE requests should all be nullable
///     SAASWEB39: Warning: Should have summary for documentation
///     Responses:
///     SAASWEB40: Error: Response must be public
///     SAASWEB41: Error: Response must be named with "Response" suffix
///     SAASWEB42: Error: Response must be in namespace "Infrastructure.Web.Api.Operations.Shared"
///     SAASWEB43: Error: Response must have a parameterless constructor
///     SAASWEB44: Error: Properties must have public getters and setters
///     SAASWEB45: Error: Properties should be nullable not Optional{T} for interoperability
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiLayerAnalyzer : DiagnosticAnalyzer
{
    internal static readonly Dictionary<OperationMethod, List<Type>>
        AllowableOperationReturnTypes =
            new()
            {
                {
                    OperationMethod.Post,
                    [typeof(ApiEmptyResult), typeof(ApiPostResult<,>)]
                },
                {
                    OperationMethod.Get,
                    [typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiGetResult<,>), typeof(ApiStreamResult)]
                },
                {
                    OperationMethod.Search,
                    [
                        typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiGetResult<,>),
                        typeof(ApiSearchResult<,>)
                    ]
                },
                {
                    OperationMethod.PutPatch,
                    [typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiPutPatchResult<,>)]
                },
                {
                    OperationMethod.Delete,
                    [typeof(ApiEmptyResult), typeof(ApiResult<,>), typeof(ApiDeleteResult)]
                }
            };

    internal static readonly Type[] AllowableServiceOperationReturnTypes =
    [
        typeof(ApiEmptyResult),
        typeof(ApiResult<,>),
        typeof(ApiPostResult<,>),
        typeof(ApiGetResult<,>),
        typeof(ApiSearchResult<,>),
        typeof(ApiPutPatchResult<,>),
        typeof(ApiDeleteResult),
        typeof(ApiStreamResult)
    ];

    internal static readonly DiagnosticDescriptor Rule010 = "SAASWEB10".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB010Title), nameof(Resources.SAASWEB010Description),
        nameof(Resources.SAASWEB010MessageFormat));
    internal static readonly DiagnosticDescriptor Rule011 = "SAASWEB011".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB011Title), nameof(Resources.SAASWEB011Description),
        nameof(Resources.SAASWEB011MessageFormat));
    internal static readonly DiagnosticDescriptor Rule012 = "SAASWEB012".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB012Title), nameof(Resources.SAASWEB012Description),
        nameof(Resources.SAASWEB012MessageFormat));
    internal static readonly DiagnosticDescriptor Rule013 = "SAASWEB013".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB013Title), nameof(Resources.SAASWEB013Description),
        nameof(Resources.SAASWEB013MessageFormat));
    internal static readonly DiagnosticDescriptor Rule014 = "SAASWEB014".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB014Title), nameof(Resources.SAASWEB014Description),
        nameof(Resources.SAASWEB014MessageFormat));
    internal static readonly DiagnosticDescriptor Rule015 = "SAASWEB015".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB015Title), nameof(Resources.SAASWEB015Description),
        nameof(Resources.SAASWEB015MessageFormat));
    internal static readonly DiagnosticDescriptor Rule016 = "SAASWEB016".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB016Title), nameof(Resources.SAASWEB016Description),
        nameof(Resources.SAASWEB016MessageFormat));
    internal static readonly DiagnosticDescriptor Rule017 = "SAASWEB017".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB017Title), nameof(Resources.SAASWEB017Description),
        nameof(Resources.SAASWEB017MessageFormat));
    internal static readonly DiagnosticDescriptor Rule018 = "SAASWEB018".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB018Title), nameof(Resources.SAASWEB018Description),
        nameof(Resources.SAASWEB018MessageFormat));
    internal static readonly DiagnosticDescriptor Rule019 = "SAASWEB019".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB019Title), nameof(Resources.SAASWEB019Description),
        nameof(Resources.SAASWEB019MessageFormat));
    internal static readonly DiagnosticDescriptor Rule020 = "SAASWEB020".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB020Title), nameof(Resources.SAASWEB020Description),
        nameof(Resources.SAASWEB020MessageFormat));

    internal static readonly DiagnosticDescriptor Rule030 = "SAASWEB030".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Rule031 = "SAASWEB031".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB031Title), nameof(Resources.SAASWEB031Description),
        nameof(Resources.SAASWEB031MessageFormat));
    internal static readonly DiagnosticDescriptor Rule032 = "SAASWEB032".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB032Title), nameof(Resources.SAASWEB032Description),
        nameof(Resources.SAASWEB032MessageFormat));
    internal static readonly DiagnosticDescriptor Rule033 = "SAASWEB033".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB033Title), nameof(Resources.SAASWEB033Description),
        nameof(Resources.SAASWEB033MessageFormat));
    internal static readonly DiagnosticDescriptor Rule034 = "SAASWEB034".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Rule035 = "SAASWEB035".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Rule036 = "SAASWEB036".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_Description_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeNullableNotOptional));
    internal static readonly DiagnosticDescriptor Rule037 = "SAASWEB037".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB037Title), nameof(Resources.SAASWEB037Description),
        nameof(Resources.SAASWEB037MessageFormat));
    internal static readonly DiagnosticDescriptor Rule038 = "SAASWEB038".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB038Title), nameof(Resources.SAASWEB038Description),
        nameof(Resources.SAASWEB038MessageFormat));
    internal static readonly DiagnosticDescriptor Rule039 = "SAASWEB039".GetDescriptor(DiagnosticSeverity.Warning,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB039Title), nameof(Resources.SAASWEB039Description),
        nameof(Resources.SAASWEB039MessageFormat));

    internal static readonly DiagnosticDescriptor Rule040 = "SAASWEB040".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_ClassMustBePublic),
        nameof(Resources.Diagnostic_Description_ClassMustBePublic),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustBePublic));
    internal static readonly DiagnosticDescriptor Rule041 = "SAASWEB041".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB041Title), nameof(Resources.SAASWEB041Description),
        nameof(Resources.SAASWEB041MessageFormat));
    internal static readonly DiagnosticDescriptor Rule042 = "SAASWEB042".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.SAASWEB042Title), nameof(Resources.SAASWEB042Description),
        nameof(Resources.SAASWEB042MessageFormat));
    internal static readonly DiagnosticDescriptor Rule043 = "SAASWEB043".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_Description_ClassMustHaveParameterlessConstructor),
        nameof(Resources.Diagnostic_MessageFormat_ClassMustHaveParameterlessConstructor));
    internal static readonly DiagnosticDescriptor Rule044 = "SAASWEB044".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_Description_PropertyMustBeGettableAndSettable),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeGettableAndSettable));
    internal static readonly DiagnosticDescriptor Rule045 = "SAASWEB045".GetDescriptor(DiagnosticSeverity.Error,
        AnalyzerConstants.Categories.WebApi, nameof(Resources.Diagnostic_Title_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_Description_PropertyMustBeNullableNotOptional),
        nameof(Resources.Diagnostic_MessageFormat_PropertyMustBeNullableNotOptional));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            Rule010, Rule011, Rule012, Rule013, Rule014, Rule015, Rule016, Rule017, Rule018, Rule019, Rule020,
            Rule030, Rule031, Rule032, Rule033, Rule034, Rule035, Rule036, Rule037, Rule038, Rule039,
            Rule040, Rule041, Rule042, Rule043, Rule044, Rule045);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeWebApiClass, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeRequestClass, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeResponseClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeWebApiClass(SyntaxNodeAnalysisContext context)
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

            if (RouteAttributeIsNotPresent(context, methodDeclarationSyntax, requestTypeSyntax!,
                    out var routeAttribute))
            {
                continue;
            }

            var requestType = requestTypeSyntax!.GetBaseOfType<IWebRequest>(context);
            var operationMethod = (OperationMethod)routeAttribute!.ConstructorArguments[1].Value!;
            operations.Add(methodDeclarationSyntax, new ServiceOperation(requestType!, operationMethod));
            if (OperationAndReturnsTypeDontMatch(context, methodDeclarationSyntax, operationMethod, returnType!))
            {
                continue;
            }

            var access = (AccessType)routeAttribute.ConstructorArguments[2].Value!;
            if (AuthorizeAttributePresence(context, requestTypeSyntax!, access))
            {
                continue;
            }

            var routePath = routeAttribute.ConstructorArguments[0]
                .Value?.ToString();
            operations[methodDeclarationSyntax]
                .SetRouteSegments(routePath);
        }

        RoutesHaveSamePrimaryResource(context, operations);
        RequestTypesAreNotDuplicated(context, operations);
        RoutesAndMethodsAreNotDuplicated(context, operations);
    }

    private static void AnalyzeRequestClass(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IWebRequest>(context))
        {
            return;
        }

        if (!classDeclarationSyntax.IsPublic())
        {
            context.ReportDiagnostic(Rule030, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.IsNamedEndingIn(AnalyzerConstants.RequestTypeSuffix))
        {
            context.ReportDiagnostic(Rule031, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.IsWithinNamespace(context, AnalyzerConstants.ServiceOperationTypesNamespace))
        {
            context.ReportDiagnostic(Rule032, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasRouteAttribute(context))
        {
            context.ReportDiagnostic(Rule033, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasParameterlessConstructor())
        {
            context.ReportDiagnostic(Rule034, classDeclarationSyntax);
        }

        var operationMethod = GetOperationMethod(context, classDeclarationSyntax);
        var allProperties = classDeclarationSyntax.Members.Where(member => member is PropertyDeclarationSyntax)
            .Cast<PropertyDeclarationSyntax>()
            .ToList();
        if (allProperties.HasAny())
        {
            foreach (var property in allProperties)
            {
                if (!property.HasPublicGetterAndSetter())
                {
                    context.ReportDiagnostic(Rule035, property);
                }

                if (!property.IsNullableType(context) && property.IsOptionalType(context))
                {
                    context.ReportDiagnostic(Rule036, property);
                }

                if (property.IsRequired())
                {
                    context.ReportDiagnostic(Rule037, property);
                }

                var isGetOrDeleteMethod = operationMethod.Exists()
                                          && !operationMethod.Value.CanHaveBody();
                if (isGetOrDeleteMethod
                    && !property.IsNullableType(context))
                {
                    context.ReportDiagnostic(Rule038, property);
                }
            }
        }

        var docs = classDeclarationSyntax.GetDocumentationCommentTriviaSyntax(context);
        if (docs is null
            || !docs.IsLanguageForCSharp())
        {
            context.ReportDiagnostic(Rule039, classDeclarationSyntax);
            return;
        }

        var xmlContent = docs.Content;
        var summary = xmlContent.SelectSingleElement(AnalyzerConstants.XmlDocumentation.Elements.Summary);
        if (summary is null
            || summary.IsEmptyNode())
        {
            context.ReportDiagnostic(Rule039, classDeclarationSyntax);
        }
    }

    private static void AnalyzeResponseClass(SyntaxNodeAnalysisContext context)
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

        if (classDeclarationSyntax.IsNotType<IWebResponse>(context))
        {
            return;
        }

        if (!classDeclarationSyntax.IsPublic())
        {
            context.ReportDiagnostic(Rule040, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.IsNamedEndingIn(AnalyzerConstants.ResponseTypeSuffix))
        {
            context.ReportDiagnostic(Rule041, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.IsWithinNamespace(context, AnalyzerConstants.ServiceOperationTypesNamespace))
        {
            context.ReportDiagnostic(Rule042, classDeclarationSyntax);
        }

        if (!classDeclarationSyntax.HasParameterlessConstructor())
        {
            context.ReportDiagnostic(Rule043, classDeclarationSyntax);
        }

        var allProperties = classDeclarationSyntax.Members.Where(member => member is PropertyDeclarationSyntax)
            .Cast<PropertyDeclarationSyntax>()
            .ToList();
        if (allProperties.HasAny())
        {
            foreach (var property in allProperties)
            {
                if (!property.HasPublicGetterAndSetter())
                {
                    context.ReportDiagnostic(Rule044, property);
                }

                if (!property.IsNullableType(context) && property.IsOptionalType(context))
                {
                    context.ReportDiagnostic(Rule045, property);
                }
            }
        }
    }

    private static bool OperationAndReturnsTypeDontMatch(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, OperationMethod operationMethod,
        ITypeSymbol returnType)
    {
        var allowedReturnTypes = AllowableOperationReturnTypes[operationMethod];

        if (MatchesAllowedTypes(context, returnType, allowedReturnTypes.ToArray()))
        {
            return false;
        }

        context.ReportDiagnostic(Rule016, methodDeclarationSyntax, operationMethod,
            allowedReturnTypes.ToArray().Stringify());

        return true;
    }

    private static OperationMethod? GetOperationMethod(SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclarationSyntax)
    {
        var requestTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
        var attribute = requestTypeSymbol.GetAttributeOfType<RouteAttribute>(context);
        if (attribute is null)
        {
            return null;
        }

        var operation = attribute.ConstructorArguments[1].Value!.ToString()!;

        if (!Enum.TryParse<OperationMethod>(operation, true, out var operationMethod))
        {
            return null;
        }

        return operationMethod;
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
            context.ReportDiagnostic(Rule010, methodDeclarationSyntax,
                AllowableServiceOperationReturnTypes.Stringify());
            return true;
        }

        if (returnType.IsVoidTask(context))
        {
            context.ReportDiagnostic(Rule010, methodDeclarationSyntax,
                AllowableServiceOperationReturnTypes.Stringify());
            return true;
        }

        if (!MatchesAllowedTypes(context, returnType, AllowableServiceOperationReturnTypes))
        {
            context.ReportDiagnostic(Rule010, methodDeclarationSyntax,
                AllowableServiceOperationReturnTypes.Stringify());
            return true;
        }

        return false;
    }

    private static bool RouteAttributeIsNotPresent(SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclarationSyntax, ParameterSyntax requestTypeSyntax,
        out AttributeData? attribute)
    {
        var requestTypeSymbol = context.SemanticModel.GetSymbolInfo(requestTypeSyntax.Type!).Symbol!;
        attribute = requestTypeSymbol.GetAttributeOfType<RouteAttribute>(context);
        if (attribute is null)
        {
            context.ReportDiagnostic(Rule013, methodDeclarationSyntax);
            if (requestTypeSyntax.Type is IdentifierNameSyntax nameSyntax)
            {
                context.ReportDiagnostic(Rule017, nameSyntax);
            }

            return true;
        }

        return false;
    }

    private static bool AuthorizeAttributePresence(SyntaxNodeAnalysisContext context, ParameterSyntax requestTypeSyntax,
        AccessType access)
    {
        var requestTypeSymbol = context.SemanticModel.GetSymbolInfo(requestTypeSyntax.Type!).Symbol!;
        var attribute = requestTypeSymbol.GetAttributeOfType<AuthorizeAttribute>(context);
        if (access == AccessType.Anonymous)
        {
            if (attribute is not null)
            {
                if (requestTypeSyntax.Type is IdentifierNameSyntax nameSyntax)
                {
                    context.ReportDiagnostic(Rule018, nameSyntax);
                }

                return true;
            }
        }
        else
        {
            if (attribute is null)
            {
                if (requestTypeSyntax.Type is IdentifierNameSyntax nameSyntax)
                {
                    context.ReportDiagnostic(Rule019, nameSyntax);
                }

                return true;
            }
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
            context.ReportDiagnostic(Rule011, methodDeclarationSyntax);
            return true;
        }

        var firstParam = parameters.First();
        var requestType = firstParam.GetBaseOfType<IWebRequest>(context);
        if (requestType is null)
        {
            context.ReportDiagnostic(Rule011, methodDeclarationSyntax);
            return true;
        }

        requestTypeSyntax = firstParam;

        if (parameters.Count == 2)
        {
            var secondParam = parameters[1];
            if (secondParam.IsNotType<CancellationToken>(context))
            {
                context.ReportDiagnostic(Rule012, methodDeclarationSyntax);
                return true;
            }
        }

        return false;
    }

    private static void RequestTypesAreNotDuplicated(SyntaxNodeAnalysisContext context,
        Dictionary<MethodDeclarationSyntax, ServiceOperation> operations)
    {
        var duplicateRequestTypes = operations
            .GroupBy(ops => ops.Value.RequestType.ToDisplayString())
            .Where(grp => grp.Count() > 1);
        foreach (var duplicateGroup in duplicateRequestTypes)
        {
            foreach (var entry in duplicateGroup)
            {
                context.ReportDiagnostic(Rule015, entry.Key);
            }
        }
    }

    private static void RoutesAndMethodsAreNotDuplicated(SyntaxNodeAnalysisContext context,
        Dictionary<MethodDeclarationSyntax, ServiceOperation> operations)
    {
        var duplicateRoutesAndMethods = operations
            .GroupBy(ops => $"{ops.Value.Method}:{ops.Value.RouteSegments.Join("/")}")
            .Where(grp => grp.Count() > 1);
        foreach (var duplicateGroup in duplicateRoutesAndMethods)
        {
            foreach (var entry in duplicateGroup)
            {
                context.ReportDiagnostic(Rule020, entry.Key);
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
                context.ReportDiagnostic(Rule014, operation.Key);
            }
        }
    }

    /// <summary>
    ///     Determines whether the <see cref="returnType" /> is the same as one of the
    ///     <see cref="AllowableServiceOperationReturnTypes" />
    ///     , or one of the <see cref="AllowableServiceOperationReturnTypes" /> as a <see cref="Task{T}" />
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
        public ServiceOperation(ITypeSymbol requestType, OperationMethod method)
        {
            RequestType = requestType;
            Method = method;
        }

        public OperationMethod Method { get; }

        public ITypeSymbol RequestType { get; }

        public IEnumerable<string> RouteSegments { get; private set; } = Enumerable.Empty<string>();

        public void SetRouteSegments(string? routePath)
        {
            if (routePath.HasValue())
            {
                RouteSegments = routePath.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
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