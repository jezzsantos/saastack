using ArchUnitNET.Domain.Extensions;
using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;

namespace ArchitectureTesting.Common;

/// <summary>
///     Provides a base class for testing each vertical slice (subdomain) of a specific <see cref="THost" />
/// </summary>
public abstract class VerticalSlicesSpecBase<THost>
{
    private readonly List<string> _allSubdomainTypes;
    private readonly Func<string, string, string> _otherSubdomainProjectsNamespaceFormat =
        (domainName, prefix) => $@"^(((?!{domainName})[\w]+){prefix}((\.[\w]+)*))$";
    private readonly Func<string, string, string> _subdomainProjectNamespaceFormat =
        (domainName, prefix) => $@"^{domainName}{prefix}((\.[\w]+)*)$";
    protected readonly ArchitectureSpecSetup<THost> Setup;

    protected VerticalSlicesSpecBase(ArchitectureSpecSetup<THost> setup)
    {
        Setup = setup;
        _allSubdomainTypes = Setup.Architecture.Namespaces
            .Where(ns => ns.NameEndsWith(ArchitectureTestingConstants.Layers.Domain.ProjectSuffix))
            .Select(ns =>
                ns.Name.Substring(0,
                    ns.Name.IndexOf(ArchitectureTestingConstants.Layers.Domain.ProjectSuffix,
                        StringComparison.Ordinal)))
            .Distinct()
            .ToList();
    }

    [Fact]
    public void WhenAnyDomainTypeDependsOnAnotherSubdomainDomainType_ThenFails()
    {
        _allSubdomainTypes.ForEach(domainName =>
        {
            var subdomainTypes = ArchRuleDefinition.Types().That()
                .ResideInNamespace(
                    _subdomainProjectNamespaceFormat(domainName,
                        ArchitectureTestingConstants.Layers.Domain.ProjectSuffix), true)
                .As($"{domainName}{ArchitectureTestingConstants.Layers.Domain.ProjectSuffix}");
            var anyOtherSubdomainType = ArchRuleDefinition.Types().That()
                .ResideInNamespace(_otherSubdomainProjectsNamespaceFormat(domainName,
                    ArchitectureTestingConstants.Layers.Domain.ProjectSuffix), true)
                .As(ArchitectureTestingConstants.Layers.Domain.AllOthersLabel);

            ArchRuleDefinition.Types().That().Are(subdomainTypes)
                .Should().NotDependOnAny(anyOtherSubdomainType)
                .Check(Setup.Architecture);
        });
    }

    [Fact]
    public void WhenAnyApplicationTypeDependsOnAnotherSubdomainApplicationType_ThenFails()
    {
        _allSubdomainTypes.ForEach(domainName =>
        {
            var subdomainTypes = ArchRuleDefinition.Types().That()
                .ResideInNamespace(
                    _subdomainProjectNamespaceFormat(domainName,
                        ArchitectureTestingConstants.Layers.Application.ProjectSuffix), true)
                .As($"{domainName}{ArchitectureTestingConstants.Layers.Application.ProjectSuffix}");
            var anyOtherSubdomainType = ArchRuleDefinition.Types().That()
                .ResideInNamespace(_otherSubdomainProjectsNamespaceFormat(domainName,
                    ArchitectureTestingConstants.Layers.Application.ProjectSuffix), true)
                .As(ArchitectureTestingConstants.Layers.Application.AllOthersLabel);

            ArchRuleDefinition.Types().That().Are(subdomainTypes)
                .Should().NotDependOnAny(anyOtherSubdomainType)
                .Check(Setup.Architecture);
        });
    }

    [Fact]
    public void WhenAnyInfrastructureTypeDependsOnAnotherSubdomainInfrastructureType_ThenFails()
    {
        _allSubdomainTypes.ForEach(domainName =>
        {
            var subdomainTypes = ArchRuleDefinition.Types().That()
                .ResideInNamespace(
                    _subdomainProjectNamespaceFormat(domainName,
                        ArchitectureTestingConstants.Layers.Infrastructure.ProjectSuffix), true)
                .As($"{domainName}{ArchitectureTestingConstants.Layers.Infrastructure.ProjectSuffix}");
            var anyOtherSubdomainType = ArchRuleDefinition.Types().That()
                .ResideInNamespace(_otherSubdomainProjectsNamespaceFormat(domainName,
                    ArchitectureTestingConstants.Layers.Infrastructure.ProjectSuffix), true)
                .As(ArchitectureTestingConstants.Layers.Infrastructure.AllOthersLabel);

            ArchRuleDefinition.Types().That().Are(subdomainTypes)
                .Should().NotDependOnAny(anyOtherSubdomainType)
                .Check(Setup.Architecture);
        });
    }
}