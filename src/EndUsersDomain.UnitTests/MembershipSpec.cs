using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Entities;
using Domain.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EndUsersDomain.UnitTests;

[Trait("Category", "Unit")]
public class MembershipSpec
{
    private readonly Membership _membership;

    public MembershipSpec()
    {
        Mock<IRecorder> recorder = new();
        Mock<IIdentifierFactory> idFactory = new();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());

        _membership = Membership.Create(recorder.Object, idFactory.Object, _ => Result.Ok).Value;
    }

    [Fact]
    public void WhenConstructed_ThenAssigned()
    {
        _membership.IsDefault.Should().BeFalse();
        _membership.RootId.Should().BeNone();
        _membership.OrganizationId.Should().BeNone();
        _membership.Roles.HasNone().Should().BeTrue();
        _membership.Features.HasNone().Should().BeTrue();
    }

    [Fact]
    public void WhenMembershipAddedEventRaised_ThenAssigned()
    {
        var roles = Roles.Create();
        var features = Features.Create();

        _membership.As<IEventingEntity>()
            .RaiseEvent(Events.MembershipAdded.Create("arootid".ToId(),
                "anorganizationid".ToId(), true, roles, features), true);

        _membership.IsDefault.Should().BeTrue();
        _membership.RootId.Should().Be("arootid".ToId());
        _membership.OrganizationId.Should().Be("anorganizationid".ToId());
        _membership.Roles.Should().Be(roles);
        _membership.Features.Should().Be(features);
    }

    [Fact]
    public void WhenMembershipRoleAssignedEventRaised_ThenAssigned()
    {
        var role = Role.Create(TenantRoles.Member).Value;

        _membership.As<IEventingEntity>()
            .RaiseEvent(Events.MembershipRoleAssigned.Create("arootid".ToId(),
                "anorganizationid".ToId(), "amembershipid".ToId(), role), true);

        _membership.Roles.HasRole(role).Should();
    }

    [Fact]
    public void WhenMembershipFeatureAssignedEventRaised_ThenAssigned()
    {
        var feature = Feature.Create(TenantFeatures.Basic).Value;

        _membership.As<IEventingEntity>()
            .RaiseEvent(Events.MembershipFeatureAssigned.Create("arootid".ToId(),
                "anorganizationid".ToId(), "amembershipid".ToId(), feature), true);

        _membership.Features.HasFeature(feature).Should();
    }

    [Fact]
    public void WhenEnsureInvariantsAndMissingDefaultRole_ThenReturnsError()
    {
        _membership.As<IEventingEntity>()
            .RaiseEvent(Events.MembershipFeatureAssigned.Create("arootid".ToId(),
                    "anorganizationid".ToId(), "amembershipid".ToId(), Feature.Create(Membership.DefaultFeature).Value),
                true);

        var result = _membership.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.Membership_MissingDefaultRole.Format(Membership.DefaultRole.Name));
    }

    [Fact]
    public void WhenEnsureInvariantsAndMissingDefaultFeature_ThenReturnsError()
    {
        _membership.As<IEventingEntity>()
            .RaiseEvent(Events.MembershipRoleAssigned.Create("arootid".ToId(),
                "anorganizationid".ToId(), "amembershipid".ToId(), Role.Create(Membership.DefaultRole).Value), true);
        
        var result = _membership.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.Membership_MissingDefaultFeature.Format(Membership.DefaultFeature.Name));
    }
}