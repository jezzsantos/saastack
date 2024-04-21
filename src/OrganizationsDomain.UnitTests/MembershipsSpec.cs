using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace OrganizationsDomain.UnitTests;

[Trait("Category", "Unit")]
public class MembershipsSpec
{
    [Fact]
    public void WhenCreateWithNeitherUserIdNorEmail_ThenReturnsError()
    {
        var result = Memberships.Create([]);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(0);
    }

    [Fact]
    public void WhenHasMemberAndEmpty_ThenReturnsFalse()
    {
        var result = Memberships.Empty.HasMember("auserid".ToId());

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasMemberAndNotMatching_ThenReturnsFalse()
    {
        var memberships = Memberships.Create(
            [Membership.Create("anorganizationid", "anotheruserid".ToId()).Value]).Value;

        var result = memberships.HasMember("auserid".ToId());

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasMemberAndMatching_ThenReturnsTrue()
    {
        var memberships = Memberships.Create([Membership.Create("anorganizationid", "auserid".ToId()).Value])
            .Value;

        var result = memberships.HasMember("auserid".ToId());

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenAddAndUserExists_ThenReturnsOriginal()
    {
        var memberships = Memberships.Create([Membership.Create("anorganizationid", "auserid".ToId()).Value])
            .Value;

        var result = memberships.Add(Membership.Create("anorganizationid", "auserid".ToId()).Value);

        result.Count.Should().Be(1);
        result.Should().BeSameAs(memberships);
    }

    [Fact]
    public void WhenAddAndUserNotExists_ThenReturnsNew()
    {
        var memberships = Memberships.Create([Membership.Create("anorganizationid", "auserid1".ToId()).Value])
            .Value;

        var result = memberships.Add(Membership.Create("anorganizationid", "auserid2".ToId()).Value);

        result.Count.Should().Be(2);
        result.HasMember("auserid1".ToId()).Should().BeTrue();
        result.HasMember("auserid2".ToId()).Should().BeTrue();
    }

    [Fact]
    public void WhenRemoveAndUserNotExists_ThenReturnsOriginal()
    {
        var memberships = Memberships.Create([Membership.Create("anorganizationid", "auserid1".ToId()).Value])
            .Value;

        var result = memberships.Remove("auserid2".ToId());

        result.Count.Should().Be(1);
        result.Should().BeSameAs(memberships);
    }

    [Fact]
    public void WhenRemoveAndUserExists_ThenReturnsRemoved()
    {
        var memberships = Memberships.Create([Membership.Create("anorganizationid", "auserid1".ToId()).Value])
            .Value;

        var result = memberships.Remove("auserid1".ToId());

        result.Count.Should().Be(0);
    }
}