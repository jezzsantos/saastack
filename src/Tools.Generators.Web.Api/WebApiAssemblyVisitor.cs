using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.CodeAnalysis;
using Tools.Generators.Web.Api.Extensions;
using SymbolExtensions = Tools.Generators.Web.Api.Extensions.SymbolExtensions;

namespace Tools.Generators.Web.Api;

/// <summary>
///     Visits all namespaces, and types in the current assembly (only),
///     collecting, service operations of classes that are:
///     1. Derive from <see cref="_serviceInterfaceSymbol" />
///     3. That are not abstract or static
///     2. That are public or internal
///     Where the methods represent service operations, that are:
///     1. Have any method name
///     2. They return type that is not void
///     3. They have a request DTO type derived from <see cref="_webRequestInterfaceSymbol" /> as their first parameter
///     4. They may have a <see cref="CancellationToken" /> as their second parameter, but no other parameters
///     5. Their request DTO type is decorated with a <see cref="_routeAttributeSymbol" /> attribute, and have both a
///     route and operation
///     6. Their request DTO type is optionally decorated with at least one <see cref="_authorizeAttributeSymbol" />
///     attribute
/// </summary>
public class WebApiAssemblyVisitor : SymbolVisitor
{
    internal static readonly string[] IgnoredNamespaces =
        ["System", "Microsoft", "MediatR", "MessagePack", "NerdBank*"];
    private readonly INamedTypeSymbol _authorizeAttributeFeaturesSymbol;
    private readonly INamedTypeSymbol _authorizeAttributeRolesSymbol;
    private readonly INamedTypeSymbol _authorizeAttributeSymbol;
    private readonly CancellationToken _cancellationToken;
    private readonly INamedTypeSymbol _cancellationTokenSymbol;
    private readonly INamedTypeSymbol _multipartFormSymbol;
    private readonly INamedTypeSymbol _routeAttributeSymbol;
    private readonly INamedTypeSymbol _serviceInterfaceSymbol;
    private readonly INamedTypeSymbol _tenantedWebRequestInterfaceSymbol;
    private readonly INamedTypeSymbol _voidSymbol;
    private readonly INamedTypeSymbol _webRequestInterfaceSymbol;
    private readonly INamedTypeSymbol _webRequestResponseInterfaceSymbol;
    private readonly INamedTypeSymbol _webserviceAttributeSymbol;

    public WebApiAssemblyVisitor(CancellationToken cancellationToken, Compilation compilation)
    {
        _cancellationToken = cancellationToken;
        _serviceInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(IWebApiService).FullName!)!;
        _webRequestInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(IWebRequest).FullName!)!;
        _tenantedWebRequestInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(ITenantedRequest).FullName!)!;
        _webRequestResponseInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(IWebRequest<>).FullName!)!;
        _webserviceAttributeSymbol = compilation.GetTypeByMetadataName(typeof(WebServiceAttribute).FullName!)!;
        _routeAttributeSymbol = compilation.GetTypeByMetadataName(typeof(RouteAttribute).FullName!)!;
        _authorizeAttributeSymbol = compilation.GetTypeByMetadataName(typeof(AuthorizeAttribute).FullName!)!;
        _authorizeAttributeRolesSymbol = compilation.GetTypeByMetadataName(typeof(Roles).FullName!)!;
        _authorizeAttributeFeaturesSymbol = compilation.GetTypeByMetadataName(typeof(Features).FullName!)!;
        _cancellationTokenSymbol = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)!;
        _voidSymbol = compilation.GetTypeByMetadataName(typeof(void).FullName!)!;
        _multipartFormSymbol = compilation.GetTypeByMetadataName(typeof(IHasMultipartForm).FullName!)!;
    }

    public List<ServiceOperationRegistration> OperationRegistrations { get; } = [];

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (IsServiceClass())
        {
            AddRegistration(symbol);
        }

        foreach (var nestedType in symbol.GetTypeMembers())
        {
            if (!nestedType.IsClass())
            {
                continue;
            }

            _cancellationToken.ThrowIfCancellationRequested();
            nestedType.Accept(this);
        }

        return;

        bool IsServiceClass()
        {
            if (!symbol.IsClass())
            {
                return false;
            }

            if (!symbol.IsPublicOrInternalClass())
            {
                return false;
            }

            if (!symbol.IsConcreteInstanceClass())
            {
                return false;
            }

            if (!symbol.IsDerivedFrom(_serviceInterfaceSymbol))
            {
                return false;
            }

            return true;
        }
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (IsIgnoredNamespace())
        {
            return;
        }

        foreach (var namespaceOrType in symbol.GetMembers())
        {
            namespaceOrType.Accept(this);
        }

        return;

        bool IsIgnoredNamespace()
        {
            var @namespace = symbol.Name;
            if (@namespace.HasNoValue())
            {
                return false;
            }

            foreach (var ignoredNamespace in IgnoredNamespaces)
            {
                if (ignoredNamespace.EndsWith("*"))
                {
                    var prefix = ignoredNamespace.Substring(0, ignoredNamespace.Length - 1);
                    if (@namespace.StartsWith(prefix))
                    {
                        return true;
                    }
                }
                else
                {
                    if (@namespace == ignoredNamespace)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private void AddRegistration(INamedTypeSymbol symbol)
    {
        var usingNamespaces = symbol.GetUsingNamespaces();
        var constructors = GetConstructors();
        var serviceName = GetServiceName();
        var basePath = GetBasePath();
        var classRegistration = new ApiServiceClassRegistration
        {
            TypeName = serviceName,
            Constructors = constructors,
            UsingNamespaces = usingNamespaces,
            BasePath = basePath
        };

        var methods = GetServiceOperationMethods();
        foreach (var method in methods)
        {
            if (!HasRouteAttribute(method, out var routeAttribute))
            {
                continue;
            }

            HasAuthorizeAttributes(method, out var authorizeAttributes);

            var routeAttributeParameters = routeAttribute!.ConstructorArguments;
            var routePath = routeAttributeParameters[0].Value!.ToString()!;
            var operationType = FromOperationVerb(routeAttributeParameters[1].Value!.ToString()!);
            var operationAccess = FromAccessType(routeAttributeParameters[2].Value!.ToString()!);
            var operationAuthorization = FromAuthorizeAttribute(authorizeAttributes);
            var isTestingOnly = bool.Parse(routeAttributeParameters[3].Value!.ToString()!);
            var requestType = method.Parameters[0].Type;
            var requestTypeName = requestType.Name;
            var requestTypeNamespace = requestType.ContainingNamespace.ToDisplayString();
            var requestTypeIsMultiTenanted = requestType.IsDerivedFrom(_tenantedWebRequestInterfaceSymbol);
            var responseType = GetResponseType(method.Parameters[0].Type);
            var responseTypeName = responseType.Name;
            var responseTypeNamespace = responseType.ContainingNamespace.ToDisplayString();
            var methodBody = SymbolExtensions.GetMethodBody(method);
            var methodName = method.Name;
            var isAsync = method.IsAsync;
            var hasCancellationToken = method.Parameters.Length == 2;
            var isMultipart = requestType.IsDerivedFrom(_multipartFormSymbol);

            OperationRegistrations.Add(new ServiceOperationRegistration
            {
                Class = classRegistration,
                RequestDto = new TypeName(requestTypeNamespace, requestTypeName),
                IsRequestDtoTenanted = requestTypeIsMultiTenanted,
                ResponseDto = new TypeName(responseTypeNamespace, responseTypeName),
                OperationMethod = operationType,
                OperationAccess = operationAccess,
                OperationAuthorization = operationAuthorization,
                IsTestingOnly = isTestingOnly,
                IsAsync = isAsync,
                IsMultipartFormData = isMultipart,
                HasCancellationToken = hasCancellationToken,
                MethodName = methodName,
                MethodBody = methodBody,
                RoutePath = routePath
            });
        }

        return;

        string? GetBasePath()
        {
            if (!HasWebServiceAttribute(symbol, out var attributeData))
            {
                return null;
            }

            return attributeData!.ConstructorArguments[0].Value!.ToString()!;
        }

        TypeName GetServiceName()
        {
            return new TypeName(symbol.ContainingNamespace.ToDisplayString(), symbol.Name);
        }

        static OperationMethod FromOperationVerb(string? operation)
        {
            if (operation is null)
            {
                return OperationMethod.Get;
            }

            return (OperationMethod)Enum.Parse(typeof(OperationMethod), operation, true);
        }

        static AccessType FromAccessType(string? access)
        {
            if (access is null)
            {
                return AccessType.Anonymous;
            }

            return (AccessType)Enum.Parse(typeof(AccessType), access, true);
        }

        OperationAuthorization? FromAuthorizeAttribute(List<AttributeData> attributes)
        {
            if (attributes.HasNone())
            {
                return null;
            }

            var rolesAndFeaturesSets = new List<List<string>>();
            foreach (var attribute in attributes)
            {
                var ctorParameters = attribute.ConstructorArguments;
                if (ctorParameters.HasNone())
                {
                    continue;
                }

                var rolesAndFeaturesSet = new List<string>();
                rolesAndFeaturesSet.AddRange(ctorParameters
                    .Select(p =>
                    {
                        var value = p.Value!.ToString();
                        var typeName = p.Type!.Name;
                        var isRole = SymbolExtensions.IsOfType(p.Type!, _authorizeAttributeRolesSymbol);
                        if (isRole)
                        {
                            if (Enum.TryParse(value, true, out Roles role))
                            {
                                return $"{typeName}.{role.ToString()}";
                            }
                        }

                        var isFeature = SymbolExtensions.IsOfType(p.Type!, _authorizeAttributeFeaturesSymbol);
                        if (isFeature)
                        {
                            if (Enum.TryParse(value, true, out Features feature))
                            {
                                return $"{typeName}.{feature.ToString()}";
                            }
                        }

                        return null!;
                    })
                    .Where(value => value is not null));
                if (rolesAndFeaturesSet.HasAny())
                {
                    rolesAndFeaturesSets.Add(rolesAndFeaturesSet);
                }
            }

            var policyName = AuthorizeAttribute.CreatePolicyName(rolesAndFeaturesSets);
            return new OperationAuthorization(policyName);
        }

        // We assume that the request type derives from IWebRequest<TResponse>
        ITypeSymbol GetResponseType(ITypeSymbol requestType)
        {
            var requestInterface = requestType.GetBaseType(_webRequestResponseInterfaceSymbol);
            if (requestInterface is null)
            {
                return requestType;
            }

            return requestInterface.TypeArguments[0];
        }

        List<Constructor> GetConstructors()
        {
            var ctors = new List<Constructor>();
            var isInjectionCtor = false;
            if (symbol.InstanceConstructors.IsDefaultOrEmpty)
            {
                return [];
            }

            foreach (var constructor in symbol.InstanceConstructors.OrderByDescending(
                         method => method.Parameters.Length))
            {
                if (!isInjectionCtor)
                {
                    if (constructor.IsPublicInstance() && !constructor.IsParameterless())
                    {
                        isInjectionCtor = true;
                    }
                }

                var body = SymbolExtensions.GetMethodBody(constructor);
                ctors.Add(new Constructor
                {
                    IsInjectionCtor = isInjectionCtor,
                    CtorParameters = constructor.Parameters.Select(param => new ConstructorParameter
                        {
                            TypeName = new TypeName(param.Type.ContainingNamespace.ToDisplayString(), param.Type.Name),
                            VariableName = param.Name
                        })
                        .ToList(),
                    MethodBody = body
                });
            }

            return ctors;
        }

        List<IMethodSymbol> GetServiceOperationMethods()
        {
            return symbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(method =>
                {
                    if (!method.IsPublicOrInternalInstanceMethod())
                    {
                        return false;
                    }

                    if (!IsCorrectReturnType(method))
                    {
                        return false;
                    }

                    if (HasWrongSetOfParameters(method))
                    {
                        return false;
                    }

                    return true;
                })
                .ToList();
        }

        // We assume that the return type is anything but void
        bool IsCorrectReturnType(IMethodSymbol method)
        {
            return !SymbolExtensions.IsOfType(method.ReturnType, _voidSymbol);
        }

        // We assume that the method one or two params:
        // the first being a Request type (derived from IWebRequest),
        // and the second being a CancellationToken
        bool HasWrongSetOfParameters(IMethodSymbol method)
        {
            var parameters = method.Parameters;
            if (parameters.Length is < 1 or > 2)
            {
                return true;
            }

            var firstParameter = parameters[0];
            if (!firstParameter.Type.IsDerivedFrom(_webRequestInterfaceSymbol))
            {
                return true;
            }

            if (parameters.Length == 2)
            {
                var secondParameter = parameters[1];
                if (!SymbolExtensions.IsOfType(secondParameter.Type, _cancellationTokenSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        // We assume that the class can be decorated with an optional WebServiceAttribute
        bool HasWebServiceAttribute(ITypeSymbol classSymbol, out AttributeData? webServiceAttribute)
        {
            webServiceAttribute = classSymbol.GetAttribute(_webserviceAttributeSymbol);
            return webServiceAttribute is not null;
        }

        // We assume that the request DTO it is decorated with one RouteAttribute
        bool HasRouteAttribute(IMethodSymbol method, out AttributeData? routeAttribute)
        {
            var parameters = method.Parameters;
            if (parameters.Length == 0)
            {
                routeAttribute = null;
                return false;
            }

            var requestDto = method.Parameters[0].Type;
            routeAttribute = requestDto.GetAttribute(_routeAttributeSymbol);
            return routeAttribute is not null;
        }

        // We assume that the request DTO it is decorated with at least one AuthorizeAttribute
        // ReSharper disable once UnusedLocalFunctionReturnValue
        bool HasAuthorizeAttributes(IMethodSymbol method, out List<AttributeData> authorizeAttributes)
        {
            var parameters = method.Parameters;
            if (parameters.Length == 0)
            {
                authorizeAttributes = [];
                return false;
            }

            var requestDto = method.Parameters[0].Type;
            authorizeAttributes = requestDto.GetAttributes(_authorizeAttributeSymbol);
            return authorizeAttributes.HasAny();
        }
    }

    public record ServiceOperationRegistration
    {
        public ApiServiceClassRegistration Class { get; set; } = null!;

        public bool HasCancellationToken { get; set; }

        public bool IsAsync { get; set; }

        public bool IsMultipartFormData { get; set; }

        public bool IsRequestDtoTenanted { get; set; }

        public bool IsTestingOnly { get; set; }

        public string? MethodBody { get; set; }

        public string? MethodName { get; set; }

        public AccessType OperationAccess { get; set; }

        public OperationAuthorization? OperationAuthorization { get; set; }

        public OperationMethod OperationMethod { get; set; }

        public TypeName RequestDto { get; set; } = null!;

        public TypeName ResponseDto { get; set; } = null!;

        public string? RoutePath { get; set; }
    }

    public record OperationAuthorization
    {
        public OperationAuthorization(string policyName)
        {
            PolicyName = policyName;
        }

        public string PolicyName { get; }
    }

    public record TypeName
    {
        public TypeName(string @namespace, string name)
        {
            Namespace = @namespace;
            Name = name;
        }

        public string FullName => $"{Namespace}.{Name}";

        public string Name { get; }

        public string Namespace { get; }

        public virtual bool Equals(TypeName? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Name == other.Name && Namespace == other.Namespace;
        }

        public override int GetHashCode()
        {
#if NETSTANDARD2_0
            var hash = 17;
            hash = hash * 23 + Namespace.GetHashCode();
            hash = hash * 23 + Name.GetHashCode();
            return hash;
#else
            return HashCode.Combine(Namespace, Name);
#endif
        }
    }

    public record ApiServiceClassRegistration
    {
        public string? BasePath { get; set; }

        public IEnumerable<Constructor> Constructors { get; set; } = new List<Constructor>();

        public TypeName TypeName { get; set; } = null!;

        public IEnumerable<string> UsingNamespaces { get; set; } = new List<string>();
    }

    public record Constructor
    {
        public IEnumerable<ConstructorParameter> CtorParameters { get; set; } = new List<ConstructorParameter>();

        public bool IsInjectionCtor { get; set; }

        public string? MethodBody { get; set; }
    }

    public record ConstructorParameter
    {
        public TypeName TypeName { get; set; } = null!;

        public string VariableName { get; set; } = null!;
    }
}