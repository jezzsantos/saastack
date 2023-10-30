using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace CarsDomain.UnitTests;

[Trait("Category", "Unit")]
public class VehicleManagersSpec
{
    private readonly VehicleManagers _managers;

    public VehicleManagersSpec()
    {
        _managers = VehicleManagers.Create();
    }

    [Fact]
    public void WhenCreateWithEmptyId_ThenReturnsError()
    {
        var result = VehicleManagers.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreate_ThenReturnsManager()
    {
        var result = VehicleManagers.Create("amanagerid").Value;

        result.Managers.First().Should().Be("amanagerid".ToId());
    }

    [Fact]
    public void WhenCreate_ThenHasNoManagers()
    {
        _managers.Managers.Should().BeEmpty();
    }

    [Fact]
    public void WhenAddAndManagerNotExist_ThenAdds()
    {
        var result = _managers.Append("amanagerid".ToId());

        result.Should().NotBeSameAs(_managers);
        result.Managers.Count.Should().Be(1);
        result.Managers[0].Should().Be("amanagerid".ToId());
    }

    [Fact]
    public void WhenAddAndManagers_ThenAdds()
    {
        var result1 = _managers.Append("amanagerid1".ToId());
        var result2 = result1.Append("amanagerid2".ToId());
        var result3 = result2.Append("amanagerid3".ToId());

        result3.Should().NotBeSameAs(_managers);
        result3.Managers.Count.Should().Be(3);
        result3.Managers.Should().ContainInOrder("amanagerid1".ToId(), "amanagerid2".ToId(),
            "amanagerid3".ToId());
    }

    [Fact]
    public void WhenAddAndManagerAndExists_ThenDoesNotAdd()
    {
        var result1 = _managers.Append("amanagerid1".ToId());
        var result2 = result1.Append("amanagerid2".ToId());
        var result3 = result2.Append("amanagerid1".ToId());

        result3.Should().NotBeSameAs(_managers);
        result3.Managers.Count.Should().Be(2);
        result3.Managers.Should().ContainInOrder("amanagerid1".ToId(), "amanagerid2".ToId());
    }
}