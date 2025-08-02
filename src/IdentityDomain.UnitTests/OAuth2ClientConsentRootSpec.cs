using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.OAuth2.ClientConsents;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OAuth2ClientConsentRootSpec
{
    private readonly OAuth2ClientConsentRoot _consent;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;

    public OAuth2ClientConsentRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());

        _consent = OAuth2ClientConsentRoot
            .Create(_recorder.Object, _idFactory.Object, "aclientid".ToId(), "auserid".ToId()).Value;
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
    public void WhenChangeConsentByAnotherUser_ThenReturnsError()
    {
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId])
            .Value;

        var result = _consent.ChangeConsent("anotherid".ToId(), true, scopes);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OAuth2ClientConsentRoot_NotOwner);
    }

    [Fact]
    public void WhenChangeConsentWithoutOpenIdScope_ThenReturnsError()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile])
            .Value;

        var result = _consent.ChangeConsent("anotherid".ToId(), true, scopes);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OAuth2ClientConsentRoot_NotOwner);
    }

    [Fact]
    public void WhenChangeConsentToTrue_ThenConsentChanged()
    {
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId])
            .Value;

        var result = _consent.ChangeConsent("auserid".ToId(), true, scopes);

        result.Should().BeSuccess();
        _consent.IsConsented.Should().BeTrue();
        _consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenChangeConsentToFalse_ThenConsentChanged()
    {
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        _consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = _consent.ChangeConsent("auserid".ToId(), false, scopes);

        result.Should().BeSuccess();
        _consent.IsConsented.Should().BeFalse();
        _consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenChangeConsentWithSameValues_ThenDoesNothing()
    {
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        _consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = _consent.ChangeConsent("auserid".ToId(), true, scopes);

        result.Should().BeSuccess();
        _consent.IsConsented.Should().BeTrue();
        _consent.Events.Count.Should().Be(2);
        _consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenEnsureInvariants_ThenReturnsOk()
    {
        var result = _consent.EnsureInvariants();

        result.Should().BeSuccess();
    }

    [Fact]
    public void WhenRevokeByAnotherUser_ThenReturnsError()
    {
        var result = _consent.Revoke("anotherid".ToId());

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.OAuth2ClientConsentRoot_NotOwner);
    }

    [Fact]
    public void WhenRevokeAndAlreadyRevoked_ThenDoesNothing()
    {
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        _consent.ChangeConsent("auserid".ToId(), false, scopes);

        var result = _consent.Revoke("auserid".ToId());

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        _consent.Events.Count.Should().Be(2);
        _consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenRevokeAndConsented_ThenRevoked()
    {
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        _consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = _consent.Revoke("auserid".ToId());

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
        _consent.Events.Last().Should().BeOfType<ConsentChanged>();
    }

    [Fact]
    public void WhenHasConsentedToAndNotConsentedToAnything_ThenReturnsFalse()
    {
        var result = _consent.HasConsentedTo(OAuth2Scopes.Empty);

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void WhenHasConsentedToAndDifferentScopes_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;
        _consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = _consent.HasConsentedTo(OAuth2Scopes.Create([OAuth2Constants.Scopes.Email]).Value);

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public void WhenHasConsentedToAndMatchingScopes_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([
            OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email
        ]).Value;
        _consent.ChangeConsent("auserid".ToId(), true, scopes);

        var result = _consent.HasConsentedTo(OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
    }
}