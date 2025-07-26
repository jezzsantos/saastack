using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class PersonCredentialsApplicationSpec
{
    private readonly PersonCredentialsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentityServerCredentialsService> _credentialsService;

    public PersonCredentialsApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        _credentialsService = new Mock<IIdentityServerCredentialsService>();
        var identityServerProvider = new Mock<IIdentityServerProvider>();
        identityServerProvider.Setup(p => p.CredentialsService)
            .Returns(_credentialsService.Object);

        _application = new PersonCredentialsApplication(identityServerProvider.Object);
    }

    [Fact]
    public async Task WhenAuthenticateAsync_ThenAuthenticates()
    {
        var tokens = new AuthenticateTokens
        {
            AccessToken = new AuthenticationToken
            {
                ExpiresOn = null,
                Type = TokenType.AccessToken,
                Value = "avalue1"
            },
            RefreshToken = new AuthenticationToken
            {
                ExpiresOn = null,
                Type = TokenType.RefreshToken,
                Value = "avalue2"
            },
            UserId = "auserid"
        };
        _credentialsService.Setup(aks =>
                aks.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        var result =
            await _application.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None);

        result.Value.Should().Be(tokens);
        _credentialsService.Verify(aks =>
            aks.AuthenticateAsync(_caller.Object, "ausername", "apassword", CancellationToken.None));
    }

    [Fact]
    public async Task WhenRegisterPersonAsync_ThenRegisters()
    {
        var credential = new PersonCredential
        {
            User = new EndUser
            {
                Id = "auserid"
            },
            Id = "anid"
        };
        _credentialsService.Setup(aks =>
                aks.RegisterPersonAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var result =
            await _application.RegisterPersonAsync(_caller.Object, "aninvitationtoken", "afirstname", "alastname",
                "anemailaddress", "apassword", "atimezone", "acountrycode", true, CancellationToken.None);

        result.Value.Should().Be(credential);
        _credentialsService.Verify(aks => aks.RegisterPersonAsync(_caller.Object, "aninvitationtoken", "afirstname",
            "alastname", "anemailaddress", "apassword", "atimezone", "acountrycode", true, CancellationToken.None));
    }

    [Fact]
    public async Task WhenConfirmPersonRegistrationAsync_ThenConfirms()
    {
        _credentialsService.Setup(aks =>
                aks.ConfirmPersonRegistrationAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.ConfirmPersonRegistrationAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeSuccess();
        _credentialsService.Verify(aks =>
            aks.ConfirmPersonRegistrationAsync(_caller.Object, "atoken", CancellationToken.None));
    }
}