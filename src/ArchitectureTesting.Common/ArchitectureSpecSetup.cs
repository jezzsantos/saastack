using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Syntax.Elements.Types;
using ArchUnitNET.Loader;

namespace ArchitectureTesting.Common;

// ReSharper disable once ClassNeverInstantiated.Global
public class ArchitectureSpecSetup<THost>
{
    /// <summary>
    ///     Provides a base class for all architecture tests, that works on all the assemblies that are loaded and reachable
    ///     from the specified <see cref="THost" />.
    ///     Note: We must NOT cache the properties like <see cref="DomainLayer" /> between tests.
    /// </summary>
    public ArchitectureSpecSetup()
    {
        var hostAssembly = typeof(THost).Assembly;
        var assemblyLocation = Path.GetDirectoryName(hostAssembly.Location);

        Architecture = new ArchLoader()
            .LoadFilteredDirectory(assemblyLocation, "*.dll")
            .Build();
    }

    public GivenTypesConjunctionWithDescription ApplicationLayer => ArchRuleDefinition.Types(true).That()
        .ResideInNamespace(ArchitectureTestingConstants.Layers.Application.SubdomainProjectNamespaces, true)
        .Or().ResideInNamespace(ArchitectureTestingConstants.Layers.Application.PlatformProjectNamespaces, true)
        .As(ArchitectureTestingConstants.Layers.Application.DisplayName);

    public Architecture Architecture { get; }

    public GivenTypesConjunctionWithDescription DomainLayer => ArchRuleDefinition.Types(true).That()
        .ResideInNamespace(ArchitectureTestingConstants.Layers.Domain.SubdomainProjectNamespaces, true)
        .Or().ResideInNamespace(ArchitectureTestingConstants.Layers.Domain.PlatformProjectNamespaces, true)
        .As(ArchitectureTestingConstants.Layers.Domain.DisplayName);

    public GivenTypesConjunctionWithDescription InfrastructureLayer => ArchRuleDefinition.Types(true).That()
        .ResideInNamespace(ArchitectureTestingConstants.Layers.Infrastructure.SubdomainProjectNamespaces, true)
        .Or().ResideInNamespace(ArchitectureTestingConstants.Layers.Infrastructure.PlatformProjectsNamespaces, true)
        .Or().ResideInNamespace(ArchitectureTestingConstants.Layers.Infrastructure.ApiHostProjectsNamespaces, true)
        .As(ArchitectureTestingConstants.Layers.Infrastructure.DisplayName);
}