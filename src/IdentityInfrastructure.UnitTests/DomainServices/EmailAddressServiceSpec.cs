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
    private readonly Mock<IPasswordHasherService> _passwordHasherService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IPasswordCredentialsRepository> _repository;
    private readonly IEmailAddressService _service;
    private readonly Mock<IConfigurationSettings> _settings;
    private readonly Mock<ITokensService> _tokensService;

    public EmailAddressServiceSpec()
    {
        _repository = new Mock<IPasswordCredentialsRepository>();
        _recorder = new Mock<IRecorder>();
        _emailAddressService = new Mock<IEmailAddressService>();
        _emailAddressService.Setup(es => es.EnsureUniqueAsync(It.IsAny<EmailAddress>(), It.IsAny<Identifier>()))
            .ReturnsAsync(true);
        _tokensService = new Mock<ITokensService>();
        _passwordHasherService = new Mock<IPasswordHasherService>();
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
        _repository.Setup(s => s.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<PasswordCredentialRoot>
                .None);

        var result = await _service.EnsureUniqueAsync(EmailAddress.Create("auser@company.com").Value, "auserid".ToId());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEnsureUniqueAsyncAndMatchesUserId_ThenReturnsTrue()
    {
        var credential = CreateCredential("auserid");
        _repository.Setup(s => s.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result = await _service.EnsureUniqueAsync(EmailAddress.Create("auser@company.com").Value, "auserid".ToId());

        result.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEnsureUniqueAsyncAndNotMatchesUserId_ThenReturnsFalse()
    {
        var credential = CreateCredential("anotheruserid");
        _repository.Setup(s => s.FindCredentialsByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential.ToOptional());

        var result = await _service.EnsureUniqueAsync(EmailAddress.Create("auser@company.com").Value, "auserid".ToId());

        result.Should().BeFalse();
    }

    private PasswordCredentialRoot CreateCredential(string userId)
    {
        var credential = PasswordCredentialRoot.Create(_recorder.Object, "acredentialid".ToIdentifierFactory(),
            _settings.Object, _emailAddressService.Object, _tokensService.Object, _passwordHasherService.Object,
            userId.ToId()).Value;
        credential.SetPasswordCredential("apassword");
        credential.SetRegistrationDetails(EmailAddress.Create("auser@company.com").Value,
            PersonDisplayName.Create("aname").Value);
        return credential;
    }
}