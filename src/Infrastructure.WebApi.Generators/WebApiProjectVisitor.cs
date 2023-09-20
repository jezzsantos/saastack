using Common.Extensions;
using Infrastructure.WebApi.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Infrastructure.WebApi.Generators;

/// <summary>
///     Visits all namespaces, and types in the current assembly (only),
///     collecting, service operations of classes that are:
///     1. Derive from <see cref="_serviceInterfaceSymbol" />
///     3. That are not abstract or static
///     2. That are public or internal
///     Where the methods represent service operations, that are:
///     1. Named either: Get, Post, Put, PutPatch or Delete
///     2. They return the type <see cref="_webHandlerResponseSymbol" />
///     3. They have a request dto type <see cref="_webRequestInterfaceSymbol" /> as their first parameter
///     4. They may have a <see cref="CancellationToken" /> as their second parameter, and no other parameters
///     5. Are decorated with the <see cref="_webRouteAttributeSymbol" /> attribute, and have both a route and operation
/// </summary>
public class WebApiProjectVisitor : SymbolVisitor
{
    private static readonly string[] IgnoredNamespaces =
        { "System", "Microsoft", "MediatR", "MessagePack", "NerdBank*" };

    private static readonly string[] SupportedServiceOperationNames =
        { "Get", "Post", "Put", "Patch", "PutPatch", "Delete" };

    private readonly CancellationToken _cancellationToken;
    private readonly INamedTypeSymbol _cancellationTokenSymbol;
    private readonly INamedTypeSymbol _serviceInterfaceSymbol;
    private readonly INamedTypeSymbol _webHandlerResponseSymbol;
    private readonly INamedTypeSymbol _webRequestInterfaceSymbol;
    private readonly INamedTypeSymbol _webRouteAttributeSymbol;

    public WebApiProjectVisitor(CancellationToken cancellationToken, Compilation compilation)
    {
        _cancellationToken = cancellationToken;
        _serviceInterfaceSymbol = compilation.GetTypeByMetadataName(typeof(IWebApiService).FullName!)!;
        _webRequestInterfaceSymbol =
            compilation.GetTypeByMetadataName(
                "Infrastructure.WebApi.Interfaces.IWebRequest`1")
            !; //HACK: we cannot reference the real type here, as it causes runtime issues. See the README.md for more details
        _webRouteAttributeSymbol = compilation.GetTypeByMetadataName(typeof(WebApiRouteAttribute).FullName!)!;
        _cancellationTokenSymbol = compilation.GetTypeByMetadataName(typeof(CancellationToken).FullName!)!;
        _webHandlerResponseSymbol = compilation.GetTypeByMetadataName(typeof(Task<>).FullName!)!
            .Construct(compilation.GetTypeByMetadataName(
                    "Microsoft.AspNetCore.Http.IResult")
                !); //HACK: we cannot reference the real type here, as it causes runtime issues. See the README.md for more details
    }

    public List<ApiServiceOperationRegistration> OperationRegistrations { get; } = new();


    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        if (IsIgnoredNamespace())
        {
            return;
        }

        foreach (var namespaceOrType in symbol.GetMembers())
        {
            _cancellationToken.ThrowIfCancellationRequested();
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

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (IsServiceClass())
        {
            AddRegistration(symbol);
        }

        foreach (var nestedType in symbol.GetTypeMembers())
        {
            if (IsNotClass(nestedType))
            {
                continue;
            }

            _cancellationToken.ThrowIfCancellationRequested();
            nestedType.Accept(this);
        }

        return;

        bool IsServiceClass()
        {
            if (IsNotClass(symbol))
            {
                return false;
            }

            var accessibility = symbol.DeclaredAccessibility;
            if (accessibility != Accessibility.Public &&
                accessibility != Accessibility.Internal)
            {
                return false;
            }

            if (symbol is not { IsAbstract: false, IsStatic: false })
            {
                return false;
            }

            if (IsIncorrectDerivedType(symbol))
            {
                return false;
            }

            return true;
        }

        bool IsIncorrectDerivedType(INamedTypeSymbol @class)
        {
            return !@class.AllInterfaces.Any(@interface =>
                SymbolEqualityComparer.Default.Equals(@interface, _serviceInterfaceSymbol));
        }

        bool IsNotClass(ITypeSymbol type)
        {
            return type.TypeKind != TypeKind.Class;
        }
    }

    private void AddRegistration(INamedTypeSymbol symbol)
    {
        var usingNamespaces = GetUsingNamespaces();
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
            var routeAttribute = GetRouteAttribute(method);
            if (routeAttribute is null)
            {
                continue;
            }

            var attributeParameters = routeAttribute.ConstructorArguments;
            var routePath = attributeParameters[0].Value!.ToString()!;
            var operationType = attributeParameters.Length >= 2
                ? FromOperationVerb(attributeParameters[1].Value!.ToString()!)
                : WebApiOperation.Get;
            var isTestingOnly = attributeParameters.Length >= 3
                ? bool.Parse(attributeParameters[2].Value!.ToString()!)
                : false;
            var requestTypeName = method.Parameters[0].Type.Name;
            var requestTypeNamespace = method.Parameters[0].Type.ContainingNamespace.ToDisplayString();
            var requestMethodBody = GetMethodBody(method);
            var requestMethodName = method.Name;

            OperationRegistrations.Add(new ApiServiceOperationRegistration
            {
                Class = classRegistration,
                RequestDtoType = new TypeName(requestTypeNamespace, requestTypeName),
                OperationType = operationType,
                IsTestingOnly = isTestingOnly,
                MethodName = requestMethodName,
                MethodBody = requestMethodBody,
                RoutePath = routePath
            });
        }

        return;

        TypeName GetServiceName()
        {
            return new TypeName(symbol.ContainingNamespace.ToDisplayString(), symbol.Name);
        }

        static string GetMethodBody(ISymbol method)
        {
            var syntaxReference = method.DeclaringSyntaxReferences.FirstOrDefault();

            var syntax = syntaxReference?.GetSyntax();
            if (syntax is MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return methodDeclarationSyntax.Body?.ToFullString() ?? string.Empty;
            }

            return string.Empty;
        }

        static WebApiOperation FromOperationVerb(string? operation)
        {
            if (operation is null)
            {
                return WebApiOperation.Get;
            }

            return Enum.Parse<WebApiOperation>(operation, true);
        }

        List<Constructor> GetConstructors()
        {
            var ctors = new List<Constructor>();
            var isInjectionCtor = false;
            foreach (var constructor in symbol.InstanceConstructors.OrderByDescending(
                         method => method.Parameters.Length))
            {
                if (!isInjectionCtor)
                {
                    if (constructor is
                        { IsStatic: false, DeclaredAccessibility: Accessibility.Public, Parameters.Length: > 0 })
                    {
                        isInjectionCtor = true;
                    }
                }

                var body = constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().ToFullString();
                ctors.Add(new Constructor
                {
                    IsInjectionCtor = isInjectionCtor,
                    CtorParameters = constructor.Parameters
                        .Select(param => new ConstructorParameter
                        {
                            TypeName = new TypeName(param.Type.ContainingNamespace.ToDisplayString(), param.Type.Name),
                            VariableName = param.Name
                        }).ToList(),
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
                    var methodName = method.Name;
                    if (IsUnsupportedMethodName(methodName))
                    {
                        return false;
                    }

                    if (IsIncorrectReturnType(method))
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

        List<string> GetUsingNamespaces()
        {
            var syntaxReference = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxReference is null)
            {
                return new List<string>();
            }

            var usingSyntaxes = syntaxReference.SyntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>();

            return usingSyntaxes
                .Select(us => us.Name!.ToString())
                .Distinct()
                .OrderDescending()
                .ToList();
        }

        AttributeData? GetRouteAttribute(ISymbol method)
        {
            return method.GetAttributes()
                .FirstOrDefault(attribute =>
                    SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, _webRouteAttributeSymbol));
        }

        bool IsUnsupportedMethodName(string methodName)
        {
            return !SupportedServiceOperationNames.Contains(methodName);
        }

        bool IsIncorrectReturnType(IMethodSymbol method)
        {
            return !SymbolEqualityComparer.Default.Equals(method.ReturnType, _webHandlerResponseSymbol);
        }

        bool HasWrongSetOfParameters(IMethodSymbol method)
        {
            var parameters = method.Parameters;
            if (parameters.Length is < 1 or > 2)
            {
                return true;
            }

            var firstParameter = parameters[0];
            if (!firstParameter.Type.AllInterfaces.Any(@interface =>
                    SymbolEqualityComparer.Default.Equals(@interface.OriginalDefinition,
                        _webRequestInterfaceSymbol)))
            {
                return true;
            }

            if (parameters.Length == 2)
            {
                var secondParameter = parameters[1];
                if (!SymbolEqualityComparer.Default.Equals(secondParameter.Type, _cancellationTokenSymbol))
                {
                    return true;
                }
            }

            return false;
        }
    }


    public record ApiServiceOperationRegistration
    {
        public required ApiServiceClassRegistration Class { get; set; }

        public required string RoutePath { get; set; }

        public required WebApiOperation OperationType { get; set; }

        public required bool IsTestingOnly { get; set; }

        public required TypeName RequestDtoType { get; set; }

        public required string MethodName { get; set; }

        public string? MethodBody { get; set; }
    }

    public record TypeName
    {
        public TypeName(string @namespace, string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(@namespace);
            ArgumentException.ThrowIfNullOrEmpty(name);
            Namespace = @namespace;
            Name = name;
        }

        public string FullName => $"{Namespace}.{Name}";

        public string Name { get; }

        public string Namespace { get; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Namespace, Name);
        }

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
    }

    public record ApiServiceClassRegistration
    {
        public required TypeName TypeName { get; set; }

        public IEnumerable<Constructor> Constructors { get; set; } = new List<Constructor>();


        public IEnumerable<string> UsingNamespaces { get; set; } = new List<string>();
    }

    public record Constructor
    {
        public required bool IsInjectionCtor { get; set; }

        public IEnumerable<ConstructorParameter> CtorParameters { get; set; } = new List<ConstructorParameter>();

        public string? MethodBody { get; set; }
    }

    public record ConstructorParameter
    {
        public required TypeName TypeName { get; set; }

        public required string VariableName { get; set; }
    }
}