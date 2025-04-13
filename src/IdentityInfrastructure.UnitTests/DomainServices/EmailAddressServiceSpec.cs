using Common;
using Common.Configuration;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;
using FluentAssertions;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;
using IdentityInfrastructure.DomainServices;
using Moq;
using Xunit;

namespace IdentityInfrastructure.UnitTests.DomainServices;

[Trait("Category", "Unit")]
public class EmailAddressServiceSpec
{
    private readonly Mock<IEmailAddressService> _emailAddressService;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IMfaService> _mfaService;
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IPersonCredentialRepository> _repository;
    private readonly IEmailAddressService _service;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<ITokensService> _tokensService;

    public EmailAddressServiceSpec()
    {
        _repository = new Mock<IPersonCredentialRepository>();
        _recorder = new Mock<IRecorder>();
        _emailAddressService = new Mock<IEmailAddressService>();
        _emailAddressService.Setup(es => es.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .ReturnsAsync(true);
        _tokensService = new Mock<ITokensService>();
        _encryptionService = new Mock<IEncryptionService>();
        _passwordHasherService = new Mock<IPasswordHasherService>();
        _mfaService = new Mock<IMfaService>();
        _settings = new Mock<IConfigurationSettings>();
        _settings.Setup(s => s.Platform.GetString(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string?)null!);
        _settings.Setup(s => s.Platform.GetNumber(It.IsAny<string>(), It.IsAny<double>()))
            .Returns(5);

        _service = new EmailAddressService(_repository.Object);
    }

    [Fact]
    public async Task WhenEnsureUniqueAsyncAndNoEmailMatch_ThenReturnsTrue()
    {
        _repository.Setup(s => s.FindCredentialByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PersonCredentialRoot>
                .None);

        var result = await _service.EnsureUniqueAsync(EmailAddress.Create("auser@company.com").Value, "auserid".ToId());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEnsureUniqueAsyncAndMatchesUserId_ThenReturnsTrue()
    {
        var credential = CreateCredential("auserid");
        _repository.Setup(s => s.FindCredentialByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result = await _service.EnsureUniqueAsync(EmailAddress.Create("auser@company.com").Value, "auserid".ToId());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEnsureUniqueAsyncAndNotMatchesUserId_ThenReturnsFalse()
    {
        var credential = CreateCredential("anotheruserid");
        _repository.Setup(s => s.FindCredentialByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result = await _service.EnsureUniqueAsync(EmailAddress.Create("auser@company.com").Value, "auserid".ToId());

        result.Should().BeFalse();
    }

    private PersonCredentialRoot CreateCredential(string userId)
    {
        var credential = PersonCredentialRoot.Create(_recorder.Object, "acredentialid".ToIdentifierFactory(),
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _encryptionService.Object,
            _passwordHasherService.Object,
            _mfaService.Object, userId.ToId()).Value;
        credential.SetCredentials("apassword");
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        return credential;
    }
}