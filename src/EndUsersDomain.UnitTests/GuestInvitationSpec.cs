using Common;
using Domain.Common.ValueObjects;
using Domain.Shared;
using FluentAssertions;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace EndUsersDomain.UnitTests;

[Trait("Category", "Unit")]
public class GuestInvitationSpec
{
    private readonly EmailAddress _inviteeEmailAddress;

    public GuestInvitationSpec()
    {
        _inviteeEmailAddress = EmailAddress.Create("auser@company.com").Value;
    }

    [Fact]
    public void WhenCreateEmpty_ThenAssigned()
    {
        var invitation = GuestInvitation.Empty;

        invitation.IsInvited.Should().BeFalse();
        invitation.IsStillOpen.Should().BeFalse();
        invitation.IsAccepted.Should().BeFalse();
        invitation.Token.Should().BeNull();
        invitation.ExpiresOnUtc.Should().BeNull();
        invitation.InvitedById.Should().BeNull();
        invitation.InviteeEmailAddress.Should().BeNull();
        invitation.InvitedAtUtc.Should().BeNull();
        invitation.AcceptedEmailAddress.Should().BeNull();
        invitation.AcceptedAtUtc.Should().BeNull();
    }

    [Fact]
    public void WhenInviteAndAlreadyInvited_ThenReturnsError()
    {
        var invitation = GuestInvitation.Empty;
        invitation = invitation.Invite("atoken", _inviteeEmailAddress,
            "aninviterid".ToId()).Value;

        var result = invitation.Invite("atoken", _inviteeEmailAddress, "aninviterid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.GuestInvitation_AlreadyInvited);
    }

    [Fact]
    public void WhenInviteAndAlreadyAccepted_ThenReturnsError()
    {
        var invitation = GuestInvitation.Empty;
        invitation = invitation.Invite("atoken", _inviteeEmailAddress,
            "aninviterid".ToId()).Value;
        invitation = invitation.Accept(_inviteeEmailAddress).Value;

        var result = invitation.Invite("atoken", _inviteeEmailAddress, "aninviterid".ToId());

        result.Should().BeError(ErrorCode.RuleViolation, Resources.GuestInvitation_AlreadyInvited);
    }

    [Fact]
    public void WhenInvite_ThenIsInvited()
    {
        var invitation = GuestInvitation.Empty;

        invitation = invitation.Invite("atoken", _inviteeEmailAddress,
            "aninviterid".ToId()).Value;

        invitation.IsInvited.Should().BeTrue();
        invitation.IsStillOpen.Should().BeTrue();
        invitation.IsAccepted.Should().BeFalse();
        invitation.Token.Should().Be("atoken");
        invitation.ExpiresOnUtc.Should().BeNear(DateTime.UtcNow.Add(GuestInvitation.DefaultTokenExpiry));
        invitation.InvitedById.Should().Be("aninviterid".ToId());
        invitation.InviteeEmailAddress!.Address.Should().Be("auser@company.com");
        invitation.InvitedAtUtc.Should().BeNear(DateTime.UtcNow);
        invitation.AcceptedEmailAddress.Should().BeNull();
        invitation.AcceptedAtUtc.Should().BeNull();
    }

    [Fact]
    public void WhenRenewAndNotInvited_ThenReturnsError()
    {
        var invitation = GuestInvitation.Empty;

        var result = invitation.Renew("atoken", _inviteeEmailAddress);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.GuestInvitation_NotInvited);
    }

    [Fact]
    public void WhenRenewAndAlreadyAccepted_ThenReturnsError()
    {
        var invitation = GuestInvitation.Empty;
        invitation = invitation.Invite("atoken", _inviteeEmailAddress,
            "aninviterid".ToId()).Value;
        invitation = invitation.Accept(_inviteeEmailAddress).Value;

        var result = invitation.Renew("atoken", _inviteeEmailAddress);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.GuestInvitation_AlreadyAccepted);
    }

    [Fact]
    public void WhenRenewAndInvited_ThenIsRenewed()
    {
        var invitation = GuestInvitation.Empty;
        invitation = invitation.Invite("atoken", _inviteeEmailAddress,
            "aninviterid".ToId()).Value;

        invitation = invitation.Renew("atoken", _inviteeEmailAddress).Value;

        invitation.IsInvited.Should().BeTrue();
        invitation.IsStillOpen.Should().BeTrue();
        invitation.IsAccepted.Should().BeFalse();
        invitation.Token.Should().Be("atoken");
        invitation.ExpiresOnUtc.Should().BeNear(DateTime.UtcNow.Add(GuestInvitation.DefaultTokenExpiry));
        invitation.InvitedById.Should().Be("aninviterid".ToId());
        invitation.InviteeEmailAddress!.Address.Should().Be("auser@company.com");
        invitation.InvitedAtUtc.Should().BeNear(DateTime.UtcNow);
        invitation.AcceptedEmailAddress.Should().BeNull();
        invitation.AcceptedAtUtc.Should().BeNull();
    }

    [Fact]
    public void WhenAcceptAndNotInvited_ThenReturnsError()
    {
        var invitation = GuestInvitation.Empty;

        var result = invitation.Accept(_inviteeEmailAddress);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.GuestInvitation_NotInvited);
    }

    [Fact]
    public void WhenAcceptAndAlreadyAccepted_ThenReturnsError()
    {
        var invitation = GuestInvitation.Empty;
        invitation = invitation.Invite("atoken", _inviteeEmailAddress,
            "aninviterid".ToId()).Value;
        invitation = invitation.Accept(_inviteeEmailAddress).Value;

        var result = invitation.Accept(_inviteeEmailAddress);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.GuestInvitation_AlreadyAccepted);
    }

    [Fact]
    public void WhenAcceptAndInvited_ThenIsAccepted()
    {
        var invitation = GuestInvitation.Empty;

        invitation = invitation.Invite("atoken", _inviteeEmailAddress,
            "aninviterid".ToId()).Value;

        invitation = invitation.Accept(_inviteeEmailAddress).Value;

        invitation.IsInvited.Should().BeTrue();
        invitation.IsStillOpen.Should().BeFalse();
        invitation.IsAccepted.Should().BeTrue();
        invitation.Token.Should().Be("atoken");
        invitation.ExpiresOnUtc.Should().BeNull();
        invitation.InvitedById.Should().Be("aninviterid".ToId());
        invitation.InviteeEmailAddress!.Address.Should().Be("auser@company.com");
        invitation.InvitedAtUtc.Should().BeNear(DateTime.UtcNow);
        invitation.AcceptedAtUtc.Should().BeNear(DateTime.UtcNow);
    }
}