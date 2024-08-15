using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class SSOUserRootSpec
{
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly SSOUserRoot _user;

    public SSOUserRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _user = SSOUserRoot.Create(recorder.Object, idFactory.Object, "aprovidername",
            "auserid".ToId()).Value;
    }

    [Fact]
    public void WhenConstructed_ThenAssigned()
    {
        _user.ProviderName.Should().Be("aprovidername");
        _user.UserId.Should().Be("auserid".ToId());
    }

    [Fact]
    public void WhenAddedDetails_ThenAdds()
    {
        var expiresOn = DateTime.UtcNow;
        var token = SSOAuthToken
            .Create(SSOAuthTokenType.AccessToken, "anaccesstoken", expiresOn, _encryptionService.Object)
            .Value;
        var tokens = SSOAuthTokens.Create([token]).Value;

        var result = _user.AddDetails(tokens, EmailAddress.Create("auser@company.com").Value,
            PersonName.Create("afirstname", null).Value, Timezone.Default, Address.Default);

        result.Should().BeSuccess();
        _user.UserId.Should().Be("auserid".ToId());
        _user.EmailAddress.Value.Address.Should().Be("auser@company.com");
        _user.Name.Value.FirstName.Text.Should().Be("afirstname");
        _user.Name.Value.LastName.Should().BeNone();
        _user.Timezone.Value.Code.Should().Be(Timezones.Default);
        _user.Address.Value.CountryCode.Should().Be(CountryCodes.Default);
        _user.Tokens.Value.ToList()[0].Should().Be(token);
        _user.Events.Last().Should().BeOfType<TokensChanged>();
    }

    [Fact]
    public void WhenChangedTokensByAnotherUser_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow;
        var token = SSOAuthToken
            .Create(SSOAuthTokenType.AccessToken, "anaccesstoken", expiresOn, _encryptionService.Object)
            .Value;
        var tokens = SSOAuthTokens.Create([token]).Value;

        var result = _user.ChangeTokens("anotheruserid".ToId(), tokens);

        result.Should().BeError(ErrorCode.RoleViolation, Resources.SSOUserRoot_NotOwner);
    }

    [Fact]
    public void WhenChangedTokens_ThenChanges()
    {
        var expiresOn = DateTime.UtcNow;
        var token = SSOAuthToken
            .Create(SSOAuthTokenType.AccessToken, "anaccesstoken", expiresOn, _encryptionService.Object)
            .Value;
        var tokens = SSOAuthTokens.Create([token]).Value;

        var result = _user.ChangeTokens("auserid".ToId(), tokens);

        result.Should().BeSuccess();
        _user.UserId.Should().Be("auserid".ToId());
        _user.Tokens.Value.ToList()[0].Should().Be(token);
        _user.Events.Last().Should().BeOfType<TokensChanged>();
    }
}