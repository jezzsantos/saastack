using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class IdentityApplicationSpec
{
    private readonly IdentityApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentityServerCredentialsService> _personCredentialsService;

    public IdentityApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId)
            .Returns("acallerid");
        _personCredentialsService = new Mock<IIdentityServerCredentialsService>();
        var identityServerProvider = new Mock<IIdentityServerProvider>();
        identityServerProvider.Setup(p => p.CredentialsService)
            .Returns(_personCredentialsService.Object);
        _application = new IdentityApplication(identityServerProvider.Object);
    }

    [Fact]
    public async Task WhenGetIdentityAsyncAndCredentialNotExist_ThenReturnsIdentity()
    {
        _personCredentialsService.Setup(pcs =>
                pcs.GetPersonCredentialForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.GetIdentityAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("acallerid");
        result.Value.IsMfaEnabled.Should().BeFalse();
        result.Value.HasCredentials.Should().BeFalse();
    }

    [Fact]
    public async Task WhenGetIdentityAsyncAndCredentialExists_ThenReturnsIdentity()
    {
        _personCredentialsService.Setup(pcs =>
                pcs.GetPersonCredentialForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersonCredential
            {
                Id = "auserid",
                User = new EndUser
                {
                    Id = "auserid"
                },
                IsMfaEnabled = true
            });

        var result = await _application.GetIdentityAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Id.Should().Be("acallerid");
        result.Value.IsMfaEnabled.Should().BeTrue();
        result.Value.HasCredentials.Should().BeTrue();
    }
}