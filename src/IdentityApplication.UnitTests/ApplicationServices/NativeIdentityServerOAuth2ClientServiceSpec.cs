using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;
using Moq;
using UnitTesting.Common;
using Xunit;
using OAuth2ClientReadModel = IdentityApplication.Persistence.ReadModels.OAuth2Client;

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerOAuth2ClientServiceSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IOAuth2ClientRepository> _clientRepository;
    private readonly Mock<IOAuth2ClientConsentRepository> _consentRepository;
    private readonly Mock<IIdentifierFactory> _identifierFactory;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly NativeIdentityServerOAuth2ClientService _service;
    private readonly Mock<ITokensService> _tokensService;

    public NativeIdentityServerOAuth2ClientServiceSpec()
    {
        _recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("auserid");
        _identifierFactory = new Mock<IIdentifierFactory>();
        _identifierFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _clientRepository = new Mock<IOAuth2ClientRepository>();
        _clientRepository.Setup(rep => rep.SaveAsync(It.IsAny<OAuth2ClientRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuth2ClientRoot root, CancellationToken _) => root);
        _consentRepository = new Mock<IOAuth2ClientConsentRepository>();
        _consentRepository.Setup(rep =>
                rep.SaveAsync(It.IsAny<OAuth2ClientConsentRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OAuth2ClientConsentRoot root, CancellationToken _) => root);
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateOAuth2ClientSecret())
            .Returns("1234567890123456789012345678901234567890123");
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _passwordHasherService.Setup(phs => phs.HashPassword(It.IsAny<string>()))
            .Returns((string value) => value);

        _service = new NativeIdentityServerOAuth2ClientService(_recorder.Object, _identifierFactory.Object,
            _tokensService.Object, _passwordHasherService.Object, _clientRepository.Object, _consentRepository.Object);
    }

    [Fact]
    public async Task WhenCreateClientAsync_ThenCreates()
    {
        var result =
            await _service.CreateClientAsync(_caller.Object, "aclientname", "aredirecturi", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Name.Should().Be("aclientname");
        result.Value.RedirectUri.Should().Be("aredirecturi");
        _clientRepository.Verify(rep => rep.SaveAsync(It.Is<OAuth2ClientRoot>(client =>
            client.Name.Value == "aclientname"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetClientAsync_ThenReturnsClient()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        _clientRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        var result = await _service.GetClientAsync(_caller.Object, "aclientid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Name.Should().Be("aclientname");
    }

    [Fact]
    public async Task WhenUpdateClientAsync_ThenUpdates()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        _clientRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        var result = await _service.UpdateClientAsync(_caller.Object, "aclientid", "anewclientname", "aredirecturi",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Name.Should().Be("anewclientname");
        result.Value.RedirectUri.Should().Be("aredirecturi");
        _clientRepository.Verify(rep => rep.SaveAsync(It.Is<OAuth2ClientRoot>(root =>
            root.Name.Value == "anewclientname"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeleteClientAsync_ThenDeletes()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        _clientRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        var result = await _service.DeleteClientAsync(_caller.Object, "aclientid", CancellationToken.None);

        result.Should().BeSuccess();
        _clientRepository.Verify(rep => rep.SaveAsync(It.Is<OAuth2ClientRoot>(root =>
            root.IsDeleted
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllClientsAsync_ThenReturnsClients()
    {
        var clients = new List<OAuth2ClientReadModel>
        {
            new() { Id = "aclientid1", Name = "aclientname1", RedirectUri = "aredirecturi1" },
            new() { Id = "aclientid2", Name = "aclientname2", RedirectUri = "aredirecturi2" }
        };
        _clientRepository.Setup(rep => rep.SearchAllAsync(It.IsAny<SearchOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QueryResults<OAuth2ClientReadModel>(clients));

        var result = await _service.SearchAllClientsAsync(_caller.Object, new SearchOptions(), new GetOptions(),
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Results.Should().HaveCount(2);
        result.Value.Results[0].Id.Should().Be("aclientid1");
        result.Value.Results[0].Name.Should().Be("aclientname1");
        result.Value.Results[0].RedirectUri.Should().Be("aredirecturi1");
        result.Value.Results[1].Id.Should().Be("aclientid2");
        result.Value.Results[1].Name.Should().Be("aclientname2");
        result.Value.Results[1].RedirectUri.Should().Be("aredirecturi2");
    }

    [Fact]
    public async Task WhenConsentToClientAsyncAndConsentExistsAndAlreadyConsented_ThenReturnsTrue()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;
        consent.ChangeConsent("auserid".ToId(), true, OAuth2Scopes.Create(OpenIdConnectConstants.Scopes.OpenId).Value);

        _clientRepository.Setup(rep => rep.LoadAsync("aclientid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.ConsentToClientAsync(_caller.Object, "aclientid", "auserid",
            OpenIdConnectConstants.Scopes.OpenId, true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsConsented.Should().BeTrue();
        _consentRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<OAuth2ClientConsentRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenConsentToClientAsyncAndConsentExistsAndNotConsented_ThenConsents()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;

        _clientRepository.Setup(rep => rep.LoadAsync("aclientid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.ConsentToClientAsync(_caller.Object, "aclientid", "auserid",
            OpenIdConnectConstants.Scopes.OpenId, true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsConsented.Should().BeTrue();
        _consentRepository.Verify(rep => rep.SaveAsync(It.Is<OAuth2ClientConsentRoot>(c =>
            c.IsConsented == true
            && c.Scopes.Items.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetConsentAsyncAndConsentDoesNotExist_ThenReturnsError()
    {
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OAuth2ClientConsentRoot>.None);

        var result = await _service.GetConsentAsync(_caller.Object, "aclientid", "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenGetConsentAsyncAndIsNotConsented_ThenReturnsFalse()
    {
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.GetConsentAsync(_caller.Object, "aclientid", "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsConsented.Should().BeFalse();
    }

    [Fact]
    public async Task WhenGetConsentAsyncAndIsConsented_ThenReturnsTrue()
    {
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;
        consent.ChangeConsent("auserid".ToId(), true, OAuth2Scopes.Create(OpenIdConnectConstants.Scopes.OpenId).Value);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.GetConsentAsync(_caller.Object, "aclientid", "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.IsConsented.Should().BeTrue();
    }

    [Fact]
    public async Task WhenRegenerateClientSecretAsyncAndClientRepositoryLoadFails_ThenReturnsError()
    {
        _clientRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected());

        var result = await _service.RegenerateClientSecretAsync(_caller.Object, "aclientid", CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected);
    }

    [Fact]
    public async Task WhenRegenerateClientSecretAsyncAndClientRepositorySaveFails_ThenReturnsError()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;
        _clientRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _clientRepository.Setup(rep => rep.SaveAsync(It.IsAny<OAuth2ClientRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected());

        var result = await _service.RegenerateClientSecretAsync(_caller.Object, "aclientid", CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected);
    }

    [Fact]
    public async Task WhenRegenerateClientSecretAsync_ThenRegeneratesSecretAndReturnsClientWithSecret()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;
        _clientRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        var result = await _service.RegenerateClientSecretAsync(_caller.Object, "aclientid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Name.Should().Be("aclientname");
        result.Value.Secret.Should().Be("1234567890123456789012345678901234567890123");
        result.Value.ExpiresOnUtc.Should().BeNull();
        _clientRepository.Verify(rep => rep.SaveAsync(It.Is<OAuth2ClientRoot>(c =>
            c.Id == client.Id && c.Secrets.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenRevokeConsentAsyncAndConsentDoesNotExist_ThenReturnsError()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        _clientRepository.Setup(rep => rep.LoadAsync("aclientid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OAuth2ClientConsentRoot>.None);

        var result = await _service.RevokeConsentAsync(_caller.Object, "aclientid", "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _consentRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<OAuth2ClientConsentRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRevokeConsentAsyncAndNotConsented_ThenDoesNothing()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;

        _clientRepository.Setup(rep => rep.LoadAsync("aclientid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.RevokeConsentAsync(_caller.Object, "aclientid", "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        _consentRepository.Verify(
            rep => rep.SaveAsync(It.IsAny<OAuth2ClientConsentRoot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRevokeConsentAsyncAndConsented_ThenDoesNothing()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object,
            Name.Create("aclientname").Value).Value;
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;
        consent.ChangeConsent("auserid".ToId(), true, OAuth2Scopes.Create(OpenIdConnectConstants.Scopes.OpenId).Value);

        _clientRepository.Setup(rep => rep.LoadAsync("aclientid".ToId(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.RevokeConsentAsync(_caller.Object, "aclientid", "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        _consentRepository.Verify(rep => rep.SaveAsync(It.Is<OAuth2ClientConsentRoot>(root =>
            root.IsConsented == false
            && root.Scopes == OAuth2Scopes.Empty
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHasClientConsentedUserAsyncConsentDoesNotExist_ThenReturnsFalse()
    {
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OAuth2ClientConsentRoot>.None);

        var result = await _service.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid", "ascope",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task WhenHasClientConsentedUserAsyncAndIsNotConsented_ThenReturnsFalse()
    {
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
            OAuth2Constants.Scopes.Email, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task WhenHasClientConsentedUserAsyncAndIsConsentedWithDifferentScope_ThenReturnsFalse()
    {
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;
        consent.ChangeConsent("auserid".ToId(), true, OAuth2Scopes.Create(OAuth2Constants.Scopes.Profile).Value);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
            OAuth2Constants.Scopes.Email, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task WhenHasClientConsentedUserAsyncAndIsConsentedWithSameScope_ThenReturnsTrue()
    {
        var consent = OAuth2ClientConsentRoot.Create(_recorder.Object, _identifierFactory.Object,
            "aclientid".ToId(), "auserid".ToId()).Value;
        consent.ChangeConsent("auserid".ToId(), true,
            OAuth2Scopes.Create([
                OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email
            ]).Value);
        _consentRepository.Setup(rep =>
                rep.FindByUserId(It.IsAny<Identifier>(), It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(consent.ToOptional());

        var result = await _service.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
            OAuth2Constants.Scopes.Profile, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task WhenVerifyClientAsyncAndMatchingSecret_ThenVerifies()
    {
        var client = OAuth2ClientRoot.Create(_recorder.Object, _identifierFactory.Object, _tokensService.Object,
            _passwordHasherService.Object, Name.Create("aclientname").Value).Value;
        client.ChangeRedirectUri("aredirecturi");
        client.GenerateSecret(Optional<TimeSpan>.None);
        _clientRepository.Setup(rep => rep.LoadAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _passwordHasherService.Setup(phs => phs.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);

        var result = await _service.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("anid");
        result.Value.Name.Should().Be("aclientname");
        result.Value.RedirectUri.Should().Be("aredirecturi");
    }
}