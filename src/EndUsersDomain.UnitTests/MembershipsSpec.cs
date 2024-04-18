using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.Organizations;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EndUsersDomain.UnitTests;

[Trait("Category", "Unit")]
public class MembershipsSpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Memberships _memberships;
    private readonly Mock<IRecorder> _recorder;

    public MembershipsSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());

        _memberships = new Memberships();
    }

    [Fact]
    public void WhenConstructed_ThenEmpty()
    {
        var result = _memberships.Count;

        result.Should().Be(0);
    }

    [Fact]
    public void WhenAddMembership_ThenAdds()
    {
        var membership = CreateMembership();

        _memberships.Add(membership);

        _memberships.Count.Should().Be(1);
        _memberships[0].Should().Be(membership);
    }

    [Fact]
    public void WhenAddMembershipAndHasSameOrganizationAsExisting_ThenReplaces()
    {
        var membership1 = CreateMembership();
        var membership2 = CreateMembership();

        _memberships.Add(membership1);
        _memberships.Add(membership2);

        _memberships.Count.Should().Be(1);
        _memberships[0].Should().Be(membership2);
    }

    [Fact]
    public void WhenAddMembershipAndDifferentOrganizationAsExisting_ThenAdds()
    {
        var membership1 = CreateMembership("anorganizationid1");
        var membership2 = CreateMembership("anorganizationid2");
        _memberships.Add(membership1);

        _memberships.Add(membership2);

        _memberships.Count.Should().Be(2);
        _memberships[0].Should().Be(membership1);
        _memberships[1].Should().Be(membership2);
    }

    [Fact]
    public void WhenRemoveMembershipAndNotExists_ThenNotRemoves()
    {
        var membership = CreateMembership();

        _memberships.Remove(membership.Id);

        _memberships.Count.Should().Be(0);
    }

    [Fact]
    public void WhenRemoveMembershipAndExists_ThenRemoves()
    {
        var membership1 = CreateMembership("anorganizationid1");
        var membership2 = CreateMembership("anorganizationid2");
        _memberships.Add(membership1);
        _memberships.Add(membership2);

        _memberships.Remove(membership2.Id);

        _memberships.Count.Should().Be(1);
        _memberships[0].Should().Be(membership1);
    }

    [Fact]
    public void WhenFindByOrganizationIdAndNotExists_ThenReturnsNone()
    {
        var membership = CreateMembership();
        _memberships.Add(membership);

        var result = _memberships.FindByOrganizationId("anunknownorganizationid".ToId());

        result.Should().BeNone();
    }

    [Fact]
    public void WhenFindByOrganizationIdAndExists_ThenReturns()
    {
        var membership1 = CreateMembership("anorganizationid1");
        var membership2 = CreateMembership("anorganizationid2");
        _memberships.Add(membership1);
        _memberships.Add(membership2);

        var result = _memberships.FindByOrganizationId(membership2.OrganizationId);

        AssertionExtensions.Should(result).Be(membership2);
    }

    [Fact]
    public void WhenEnsureInvariantsAndNoDefaultMembership_ThenReturnsError()
    {
        var membership1 = CreateMembership("anorganizationid1", false);
        _memberships.Add(membership1);

        var result = _memberships.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.Memberships_NoDefault);
    }

    [Fact]
    public void WhenEnsureInvariantsAndMoreThanOneDefault_ThenReturnsError()
    {
        var membership1 = CreateMembership("anorganizationid1");
        var membership2 = CreateMembership("anorganizationid2");
        _memberships.Add(membership1);
        _memberships.Add(membership2);

        var result = _memberships.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.Memberships_MultipleDefaults);
    }

    [Fact]
    public void WhenEnsureInvariantsAndMoreThanOneForEachOrganization_ThenReturnsError()
    {
        var membership1 = CreateMembership("anorganizationid1");
        var membership2 = CreateMembership("anorganizationid2", false);
        _memberships.Add(membership1);
        _memberships.Add(membership2);
#if TESTINGONLY
        membership2.TestingOnly_ChangeOrganizationId("anorganizationid1");
#endif

        var result = _memberships.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.Memberships_DuplicateMemberships);
    }

    private Membership CreateMembership(string organizationId = "anorganizationid", bool isDefault = true)
    {
        var roles = Roles.Create(Membership.DefaultRole).Value;
        var features = Features.Create(Membership.DefaultFeature).Value;
        var membership = Membership.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok).Value;
        membership.As<IEventingEntity>()
            .RaiseEvent(Events.MembershipAdded("arootid".ToId(),
                organizationId.ToId(), OrganizationOwnership.Shared, isDefault, roles, features), true);
        return membership;
    }
}