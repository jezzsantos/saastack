using Infrastructure.Eventing.Interfaces.Notifications;
using Microsoft.CodeAnalysis;
using Tools.Generators.Workers.Extensions;

namespace Tools.Generators.Workers;

/// <summary>
///     Visits all types in all assemblies,
///     collecting, notification consumer classes:
///     1. Derive from <see cref="_notificationConsumerSymbol" />
///     3. That are not abstract or static
///     2. That are public or internal
/// </summary>
public class ApiHostModuleVisitor : SymbolVisitor
{
    private readonly CancellationToken _cancellationToken;
    private readonly List<NotificationConsumer> _consumerTypes = [];
    private readonly INamedTypeSymbol _notificationConsumerSymbol;

    public ApiHostModuleVisitor(Compilation compilation, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _notificationConsumerSymbol =
            compilation.GetTypeByMetadataName(typeof(IDomainEventNotificationConsumer).FullName!)!;
    }

    public IReadOnlyList<NotificationConsumer> ConsumerTypes => _consumerTypes;

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitModule(IModuleSymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        var referencedAssemblies = symbol.ReferencedAssemblySymbols.ToArray();
        foreach (var assembly in referencedAssemblies)
        {
            assembly.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (IsConsumerClass())
        {
            AddConsumer(symbol);
        }

        return;

        bool IsConsumerClass()
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

            if (!symbol.IsDerivedFrom(_notificationConsumerSymbol))
            {
                return false;
            }

            return true;
        }
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        foreach (var namespaceOrType in symbol.GetMembers())
        {
            namespaceOrType.Accept(this);
        }
    }

    private void AddConsumer(INamedTypeSymbol symbol)
    {
        var consumerType = GetConsumerType();

        _consumerTypes.Add(new NotificationConsumer(consumerType));

        return;

        TypeName GetConsumerType()
        {
            return new TypeName(symbol.ContainingNamespace.ToDisplayString(), symbol.Name);
        }
    }

    public class NotificationConsumer
    {
        public NotificationConsumer(TypeName name)
        {
            Name = name;
        }

        public TypeName Name { get; }
    }

    public class TypeName
    {
        public TypeName(string ns, string name)
        {
            Namespace = ns;
            Name = name;
        }

        public string Name { get; }

        public string Namespace { get; }
    }
}