using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Interfaces.Entities;
using Domain.Services.Shared.DomainServices;
using Domain.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class SSOUserRootSpec
{
    private readonly SSOUserRoot _user;

    public SSOUserRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var encryptionService = new Mock<IEncryptionService>();
        encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns((string value) => value);
        _user = SSOUserRoot.Create(recorder.Object, idFactory.Object, encryptionService.Object, "aprovidername",
            "auserid".ToId()).Value;
    }

    [Fact]
    public void WhenConstructed_ThenAssigned()
    {
        _user.ProviderName.Should().Be("aprovidername");
        _user.UserId.Should().Be("auserid".ToId());
    }

    [Fact]
    public void WhenUpdateDetails_ThenUpdates()
    {
        var expiresOn = DateTime.UtcNow;
        var tokens = SSOAuthTokens.Create(new List<SSOAuthToken>
        {
            SSOAuthToken.Create(SSOAuthTokenType.AccessToken, "anaccesstoken", expiresOn).Value
        }).Value;

        _user.UpdateDetails(tokens, EmailAddress.Create("auser@company.com").Value,
            PersonName.Create("afirstname", null).Value, Timezone.Default, Address.Default);

        _user.UserId.Should().Be("auserid".ToId());
        _user.EmailAddress.Value.Address.Should().Be("auser@company.com");
        _user.Name.Value.FirstName.Text.Should().Be("afirstname");
        _user.Name.Value.LastName.Should().BeNone();
        _user.Timezone.Value.Code.Should().Be(Timezones.Default);
        _user.Address.Value.CountryCode.Should().Be(CountryCodes.Default);
        _user.Events.Last().Should().BeOfType<TokensUpdated>();
    }
}