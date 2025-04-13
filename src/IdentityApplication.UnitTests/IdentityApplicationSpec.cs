using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class IdentityApplicationSpec
{
    private readonly IdentityApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IPersonCredentialsService> _personCredentialsService;

    public IdentityApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId)
            .Returns("acallerid");
        _personCredentialsService = new Mock<IPersonCredentialsService>();
        _application = new IdentityApplication(_personCredentialsService.Object);
    }

    [Fact]
    public async Task WhenGetIdentityAsyncAndCredentialNotExist_ThenReturnsIdentity()
    {
        _personCredentialsService.Setup(pcs =>
                pcs.GetCredentialsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<CancellationToken>()))
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
                pcs.GetCredentialsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<CancellationToken>()))
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