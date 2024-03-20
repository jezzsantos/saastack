using ArchUnitNET.Fluent;
using ArchUnitNET.xUnit;
using Xunit;

namespace ArchitectureTesting.Common;

/// <summary>
///     Provides a base class for testing the horizontal layers of a specific <see cref="THost" />
/// </summary>
public abstract class HorizontalLayersSpecBase<THost>
{
    protected readonly ArchitectureSpecSetup<THost> Setup;

    protected HorizontalLayersSpecBase(ArchitectureSpecSetup<THost> setup)
    {
        Setup = setup;
    }

    [Fact]
    public void WhenAnyDomainLayerTypeDependsOnAnyApplicationLayerType_ThenFails()
    {
        ArchRuleDefinition.Types().That().Are(Setup.DomainLayer)
            .Should().NotDependOnAny(Setup.ApplicationLayer)
            .Check(Setup.Architecture);
    }

    [Fact]
    public void WhenAnyDomainLayerTypeDependsOnAnyInfrastructureLayerType_ThenFails()
    {
        ArchRuleDefinition.Types().That().Are(Setup.DomainLayer)
            .Should().NotDependOnAny(Setup.InfrastructureLayer)
            .Check(Setup.Architecture);
    }

    [Fact]
    public void WhenAnyApplicationLayerTypeDependsOnAnyInfrastructureLayerType_ThenFails()
    {
        ArchRuleDefinition.Types().That().Are(Setup.ApplicationLayer)
            .Should().NotDependOnAny(Setup.InfrastructureLayer)
            .Check(Setup.Architecture);
    }
}