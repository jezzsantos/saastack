using Application.Interfaces;
using Application.Services.Shared;
using Common;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class PersonCredentialsApplicationPasswordResetSpec
{
    private readonly PersonCredentialsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentityServerCredentialsService> _credentialsService;

    public PersonCredentialsApplicationPasswordResetSpec()
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
    public async Task WhenInitiatePasswordRequest_ThenInitiates()
    {
        _credentialsService.Setup(aks =>
                aks.InitiatePasswordResetAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.InitiatePasswordResetAsync(_caller.Object, "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        _credentialsService.Verify(aks =>
            aks.InitiatePasswordResetAsync(_caller.Object, "anemailaddress", CancellationToken.None));
    }

    [Fact]
    public async Task WhenResendPasswordRequest_ThenResends()
    {
        _credentialsService.Setup(aks =>
                aks.ResendPasswordResetAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.ResendPasswordResetAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeSuccess();
        _credentialsService.Verify(
            aks => aks.ResendPasswordResetAsync(_caller.Object, "atoken", CancellationToken.None));
    }

    [Fact]
    public async Task WhenVerifyPasswordResetAsync_ThenVerifies()
    {
        _credentialsService.Setup(aks =>
                aks.VerifyPasswordResetAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.VerifyPasswordResetAsync(_caller.Object, "atoken", CancellationToken.None);

        result.Should().BeSuccess();
        _credentialsService.Verify(
            aks => aks.VerifyPasswordResetAsync(_caller.Object, "atoken", CancellationToken.None));
    }

    [Fact]
    public async Task WhenCompletePasswordResetAsync_ThenCompletes()
    {
        _credentialsService.Setup(aks =>
                aks.CompletePasswordResetAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.CompletePasswordResetAsync(_caller.Object, "atoken", "apassword",
                CancellationToken.None);

        result.Should().BeSuccess();
        _credentialsService.Verify(aks =>
            aks.CompletePasswordResetAsync(_caller.Object, "atoken", "apassword", CancellationToken.None));
    }
}