using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.OAuth2.ClientConsents;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OAuth2ClientConsentRootSpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;

    public OAuth2ClientConsentRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
    }

    [Fact]
    public void WhenCreate_ThenCreated()
    {
        var result =
            OAuth2ClientConsentRoot.Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId());

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid".ToId());
        result.Value.ClientId.Should().Be("aclientid".ToId());
        result.Value.UserId.Should().Be("auserid".ToId());
        result.Value.IsConsented.Should().BeFalse();
        result.Value.Scopes.Should().Be(OAuth2Scopes.Empty);
        result.Value.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenChangeConsentToTrue_ThenConsentChanged()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile])
            .Value;

        var result = consent.ChangeConsent("auserid".ToId(), true, scopes);

        result.Should().BeSuccess();
        consent.IsConsented.Should().BeTrue();
        consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenChangeConsentByAnotherUser_ThenReturnsError()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile])
            .Value;

        var result = consent.ChangeConsent("anotherid".ToId(), true, scopes);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OAuth2ClientConsentRoot_NotOwner);
    }

    [Fact]
    public void WhenChangeConsentToFalse_ThenConsentChanged()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;
        consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = consent.ChangeConsent("auserid".ToId(), false, OAuth2Scopes.Empty);

        result.Should().BeSuccess();
        consent.IsConsented.Should().BeFalse();
        consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenChangeConsentWithSameValues_ThenDoesNothing()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;
        consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = consent.ChangeConsent("auserid".ToId(), true, scopes);

        result.Should().BeSuccess();
        consent.IsConsented.Should().BeTrue();
        consent.Events.Count.Should().Be(2);
        consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenEnsureInvariants_ThenReturnsOk()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;

        var result = consent.EnsureInvariants();

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenRevokeByAnotherUser_ThenReturnsError()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;

        var result = consent.Revoke("anotherid".ToId());

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OAuth2ClientConsentRoot_NotOwner);
    }

    [Fact]
    public void WhenRevokeAndAlreadyRevoked_ThenDoesNothing()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;
        consent.ChangeConsent("auserid".ToId(), false, scopes);

        var result = consent.Revoke("auserid".ToId());

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        consent.Events.Count.Should().Be(2);
        consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenRevokeAndConsented_ThenRevoked()
    {
        var consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;
        consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = consent.Revoke("auserid".ToId());

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }
}