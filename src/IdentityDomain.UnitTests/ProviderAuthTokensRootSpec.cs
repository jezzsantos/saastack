using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.ProviderAuthTokens;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class ProviderAuthTokensRootSpec
{
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly ProviderAuthTokensRoot _tokens;

    public ProviderAuthTokensRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(x => x.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);

        _tokens = ProviderAuthTokensRoot.Create(_recorder.Object, _idFactory.Object, "aprovidername", "auserid".ToId())
            .Value;
    }

    [Fact]
    public void WhenCreate_ThenCreated()
    {
        var result =
            ProviderAuthTokensRoot.Create(_recorder.Object, _idFactory.Object, "aprovidername", "auserid".ToId());

        result.Should().BeSuccess();
        result.Value.UserId.Should().Be("auserid".ToId());
        result.Value.ProviderName.Should().Be("aprovidername");
        result.Value.Tokens.HasValue.Should().BeFalse();
    }

    [Fact]
    public void WhenChangedTokensByAnotherUserFirstTime_ThenChangesTokens()
    {
        var expiresOn = DateTime.UtcNow;
        var token = AuthToken
            .Create(AuthTokenType.AccessToken, "anaccesstoken", expiresOn, _encryptionService.Object)
            .Value;
        var tokens = AuthTokens.Create([token]).Value;

        var result = _tokens.ChangeTokens("anotheruserid".ToId(), tokens);

        result.Should().BeSuccess();
        _tokens.UserId.Should().Be("auserid".ToId());
        _tokens.Tokens.Value.ToList()[0].Should().Be(token);
        _tokens.Events.Last().Should().BeOfType<TokensChanged>();
    }

    [Fact]
    public void WhenChangedTokensByAnotherUserNextTime_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow;
        var token = AuthToken
            .Create(AuthTokenType.AccessToken, "anaccesstoken", expiresOn, _encryptionService.Object)
            .Value;
        var tokens = AuthTokens.Create([token]).Value;
        _tokens.ChangeTokens("anotheruserid".ToId(), tokens);

        var result = _tokens.ChangeTokens("anotheruserid".ToId(), tokens);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.ProviderAuthTokensRoot_NotOwner);
    }

    [Fact]
    public void WhenChangedTokensByOwnerAnyTime_ThenChanges()
    {
        var expiresOn = DateTime.UtcNow;
        var token = AuthToken
            .Create(AuthTokenType.AccessToken, "anaccesstoken", expiresOn, _encryptionService.Object)
            .Value;
        var tokens = AuthTokens.Create([token]).Value;

        var result = _tokens.ChangeTokens("auserid".ToId(), tokens);

        result.Should().BeSuccess();
        _tokens.UserId.Should().Be("auserid".ToId());
        _tokens.Tokens.Value.ToList()[0].Should().Be(token);
        _tokens.Events.Last().Should().BeOfType<TokensChanged>();
    }
}