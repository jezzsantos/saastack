using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.OAuth2.Clients;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using FluentAssertions;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OAuth2ClientRootSpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ITokensService> _tokensService;

    public OAuth2ClientRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateOAuth2ClientSecret())
            .Returns("1234567890123456789012345678901234567890123");
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(phs => phs.HashPassword(It.IsAny<string>()))
            .Returns("asecrethash");
    }

    [Fact]
    public void WhenCreate_ThenCreated()
    {
        var name = Name.Create("aclientname").Value;

        var result = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, name);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(name);
        result.Value.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenDelete_ThenDeleted()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;

        var result = client.Delete("adeleterid".ToId());

        result.Should().BeSuccess();
        client.Events.Last().Should().BeOfType<Deleted>();
    }

    [Fact]
    public void WhenNameChangedAndSameValues_ThenDoesNothing()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;

        var result = client.ChangeName(Name.Create("aclientname").Value);

        result.Should().BeSuccess();
        client.Name.Value.Text.Should().Be("aclientname");
        client.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenNameChanged_ThenChanged()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;

        var result = client.ChangeName(Name.Create("anewname").Value);

        result.Should().BeSuccess();
        client.Name.Value.Text.Should().Be("anewname");
        client.Events.Last().Should().BeOfType<NameChanged>();
    }
    [Fact]
    public void WhenRedirectUriChangedAndSameValues_ThenDoesNothing()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;
        client.ChangeRedirectUri("aredirecturi");

        var result = client.ChangeRedirectUri("aredirecturi");

        result.Should().BeSuccess();
        client.RedirectUri.Value.Should().Be("aredirecturi");
        client.Events.Count.Should().Be(2);
        client.Events.Last().Should().BeOfType<RedirectUriChanged>();
    }

    [Fact]
    public void WhenRedirectUriChanged_ThenChanged()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;

        var result = client.ChangeRedirectUri("aredirecturi");

        result.Should().BeSuccess();
        client.RedirectUri.Value.Should().Be("aredirecturi");
        client.Events.Last().Should().BeOfType<RedirectUriChanged>();
    }

    [Fact]
    public void WhenGenerateSecretWithoutDuration_ThenGeneratesSecretWithMaxExpiry()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;

        var result = client.GenerateSecret(Optional<TimeSpan>.None);

        result.Should().BeSuccess();
        result.Value.PlainSecret.Should().Be("1234567890123456789012345678901234567890123");
        result.Value.ExpiresOn.Should().BeNone();
        client.Secrets.Should().HaveCount(1);
        client.Secrets[0].SecretHash.Should().Be("asecrethash");
        client.Secrets[0].ExpiresOn.Should().BeNone();
        client.Events.Last().Should().BeOfType<SecretAdded>();
    }

    [Fact]
    public void WhenGenerateSecretWithDuration_ThenGeneratesSecretWithCalculatedExpiry()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;
        var duration = TimeSpan.FromDays(30);

        var result = client.GenerateSecret(duration);

        result.Should().BeSuccess();
        result.Value.PlainSecret.Should().Be("1234567890123456789012345678901234567890123");
        result.Value.ExpiresOn.Value.Should().BeNear(DateTime.UtcNow.Add(duration), TimeSpan.FromMinutes(1));
        client.Secrets.Should().HaveCount(1);
        client.Secrets[0].SecretHash.Should().Be("asecrethash");
        client.Secrets[0].ExpiresOn.Value.Should().BeNear(DateTime.UtcNow.Add(duration), TimeSpan.FromMinutes(1));
        client.Events.Last().Should().BeOfType<SecretAdded>();
    }
}