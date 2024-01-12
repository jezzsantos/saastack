using Infrastructure.Web.Api.Interfaces;
using Microsoft.CodeAnalysis;
using Tools.Generators.WebApi.Extensions;

namespace Tools.Generators.WebApi;

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
///     5. Their request DTO type is decorated with the <see cref="_routeAttributeSymbol" /> attribute, and have both a
///     route and operation
/// </summary>
public class WebApiAssemblyVisitor : SymbolVisitor
{
    internal static readonly string[] IgnoredNamespaces =
        ["System", "Microsoft", "MediatR", "MessagePack", "NerdBank*"];

    private readonly CancellationToken _cancellationToken;
    private readonly INamedTypeSymbol _cancellationTokenSymbol;
    private readonly INamedTypeSymbol _routeAttributeSymbol;
    private readonly INamedTypeSymbol _serviceInterfaceSymbol;
    private readonly INamedTypeSymbol _voidSymbol;
    private readonly INamedTypeSymbol _webRequestInterfaceSymbol;
    private readonly INamedTypeSymbol _webRequestResponseInterfaceSymbol;

    public WebApiAssemblyVisitor(CancellationToken cancellationToken, Compilation compilation)
    {
        _cancellationToken = cancellationToken;
        _serviceInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(IWebApiService).FullName!)!;
        _webRequestInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(IWebRequest).FullName!)!;
        _webRequestResponseInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(IWebRequest<>).FullName!)!;
        _routeAttributeSymbol = compilation.GetTypeByMetadataName(typeof(RouteAttribute).FullName!)!;
        _cancellationTokenSymbol = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)!;
        _voidSymbol = compilation.GetTypeByMetadataName(typeof(void).FullName!)!;
    }

    public List<ServiceOperationRegistration> OperationRegistrations { get; } = new();

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
        var classRegistration = new ApiServiceClassRegistration
        {
            TypeName = serviceName,
            Constructors = constructors,
            UsingNamespaces = usingNamespaces
        };

        var methods = GetServiceOperationMethods();
        foreach (var method in methods)
        {
            if (!HasRouteAttribute(method, out var routeAttribute))
            {
                continue;
            }

            var attributeParameters = routeAttribute!.ConstructorArguments;
            var routePath = attributeParameters[0].Value!.ToString()!;
            var operationType = FromOperationVerb(attributeParameters[1].Value!.ToString()!);
            var operationAccess = FromAccessType(attributeParameters[2].Value!.ToString()!);
            var isTestingOnly = bool.Parse(attributeParameters[3].Value!.ToString()!);
            var requestType = method.Parameters[0].Type;
            var requestTypeName = requestType.Name;
            var requestTypeNamespace = requestType.ContainingNamespace.ToDisplayString();
            var responseType = GetResponseType(method.Parameters[0].Type);
            var responseTypeName = responseType.Name;
            var responseTypeNamespace = responseType.ContainingNamespace.ToDisplayString();
            var methodBody = method.GetMethodBody();
            var methodName = method.Name;
            var isAsync = method.IsAsync;
            var hasCancellationToken = method.Parameters.Length == 2;

            OperationRegistrations.Add(new ServiceOperationRegistration
            {
                Class = classRegistration,
                RequestDtoType = new TypeName(requestTypeNamespace, requestTypeName),
                ResponseDtoType = new TypeName(responseTypeNamespace, responseTypeName),
                OperationType = operationType,
                OperationAccess = operationAccess,
                IsTestingOnly = isTestingOnly,
                IsAsync = isAsync,
                HasCancellationToken = hasCancellationToken,
                MethodName = methodName,
                MethodBody = methodBody,
                RoutePath = routePath
            });
        }

        return;

        TypeName GetServiceName()
        {
            return new TypeName(symbol.ContainingNamespace.ToDisplayString(), symbol.Name);
        }

        static ServiceOperation FromOperationVerb(string operation)
        {
            if (operation is null)
            {
                return ServiceOperation.Get;
            }

            return (ServiceOperation)Enum.Parse(typeof(ServiceOperation), operation, true);
        }

        static AccessType FromAccessType(string access)
        {
            if (access is null)
            {
                return AccessType.Anonymous;
            }

            return (AccessType)Enum.Parse(typeof(AccessType), access, true);
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
                return new List<Constructor>();
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

                var body = constructor.GetMethodBody();
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
            return !method.ReturnType.IsOfType(_voidSymbol);
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
                if (!secondParameter.Type.IsOfType(_cancellationTokenSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        // We assume that the request DTO it is decorated with a RouteAttribute
        bool HasRouteAttribute(IMethodSymbol method, out AttributeData routeAttribute)
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
    }

    public record ServiceOperationRegistration
    {
        public ApiServiceClassRegistration Class { get; set; }

        public bool HasCancellationToken { get; set; }

        public bool IsAsync { get; set; }

        public bool IsTestingOnly { get; set; }

        public string MethodBody { get; set; }

        public string MethodName { get; set; }

        public AccessType OperationAccess { get; set; }

        public ServiceOperation OperationType { get; set; }

        public TypeName RequestDtoType { get; set; }

        public TypeName ResponseDtoType { get; set; }

        public string RoutePath { get; set; }
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

        public virtual bool Equals(TypeName other)
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
        public IEnumerable<Constructor> Constructors { get; set; } = new List<Constructor>();

        public TypeName TypeName { get; set; }

        public IEnumerable<string> UsingNamespaces { get; set; } = new List<string>();
    }

    public record Constructor
    {
        public IEnumerable<ConstructorParameter> CtorParameters { get; set; } = new List<ConstructorParameter>();

        public bool IsInjectionCtor { get; set; }

        public string MethodBody { get; set; }
    }

    public record ConstructorParameter
    {
        public TypeName TypeName { get; set; }

        public string VariableName { get; set; }
    }
}