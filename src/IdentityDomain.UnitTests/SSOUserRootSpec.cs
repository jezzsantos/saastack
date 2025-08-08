using Common;
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
    public void WhenChangeDetailsAndSameDetails_ThenDoesNothing()
    {
        _user.ChangeDetails("aprovideruid", EmailAddress.Create("auser@company.com").Value,
            PersonName.Create("afirstname", null).Value, Timezone.Default, Locale.Default, Address.Default);

        var result = _user.ChangeDetails("aprovideruid", EmailAddress.Create("auser@company.com").Value,
            PersonName.Create("afirstname", null).Value, Timezone.Default, Locale.Default, Address.Default);

        result.Should().BeSuccess();
        _user.Events.Count.Should().Be(2);
        _user.Events[0].Should().BeOfType<Created>();
        _user.Events[1].Should().BeOfType<DetailsChanged>();
    }

    [Fact]
    public void WhenChangeDetails_ThenAdds()
    {
        var result = _user.ChangeDetails("aprovideruid", EmailAddress.Create("auser@company.com").Value,
            PersonName.Create("afirstname", null).Value, Timezone.Default, Locale.Default, Address.Default);

        result.Should().BeSuccess();
        _user.ProviderUId.Should().Be("aprovideruid");
        _user.UserId.Should().Be("auserid".ToId());
        _user.EmailAddress.Value.Address.Should().Be("auser@company.com");
        _user.Name.Value.FirstName.Text.Should().Be("afirstname");
        _user.Name.Value.LastName.Should().BeNone();
        _user.Timezone.Value.Code.Should().Be(Timezones.Default);
        _user.Locale.Value.Code.Should().Be(Locales.Default);
        _user.Address.Value.CountryCode.Should().Be(CountryCodes.Default);
        _user.Events.Count.Should().Be(2);
        _user.Events[0].Should().BeOfType<Created>();
        _user.Events[1].Should().BeOfType<DetailsChanged>();
    }
}