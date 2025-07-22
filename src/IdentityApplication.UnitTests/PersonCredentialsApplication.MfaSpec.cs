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
public class PersonCredentialsApplicationMfaSpec
{
    private readonly PersonCredentialsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentityServerCredentialsService> _credentialsService;

    public PersonCredentialsApplicationMfaSpec()
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
    public async Task WhenChangeMfaAsync_ThenChangesMfa()
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
                aks.ChangeMfaForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var result =
            await _application.ChangeMfaAsync(_caller.Object, true, CancellationToken.None);

        result.Value.Should().Be(credential);
        _credentialsService.Verify(aks =>
            aks.ChangeMfaForUserAsync(_caller.Object, "acallerid", true, CancellationToken.None));
    }

    [Fact]
    public async Task WhenListMfaAuthenticatorsAsync_ThenReturnsList()
    {
        var authenticators = new List<CredentialMfaAuthenticator>
        {
            new()
            {
                Id = "anauthenticatorid",
                Type = CredentialMfaAuthenticatorType.TotpAuthenticator,
                IsActive = true
            }
        };
        _credentialsService.Setup(aks =>
                aks.ListMfaAuthenticatorsForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(authenticators);

        var result =
            await _application.ListMfaAuthenticatorsAsync(_caller.Object, "anmfatoken", CancellationToken.None);

        result.Value.Should().BeSameAs(authenticators);
        _credentialsService.Verify(aks =>
            aks.ListMfaAuthenticatorsForUserAsync(_caller.Object, "acallerid", "anmfatoken", CancellationToken.None));
    }

    [Fact]
    public async Task WhenDisassociateMfaAuthenticatorAsync_ThenDisassociates()
    {
        _credentialsService.Setup(aks =>
                aks.DisassociateMfaAuthenticatorForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result =
            await _application.DisassociateMfaAuthenticatorAsync(_caller.Object, "anauthenticatorid",
                CancellationToken.None);

        result.Should().BeSuccess();
        _credentialsService.Verify(aks =>
            aks.DisassociateMfaAuthenticatorForUserAsync(_caller.Object, "acallerid", "anauthenticatorid",
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenAssociateMfaAuthenticatorAsync_ThenAssociates()
    {
        var association = new CredentialMfaAuthenticatorAssociation
        {
            Type = CredentialMfaAuthenticatorType.TotpAuthenticator,
            Secret = "asecret",
            BarCodeUri = "abarcodeuri"
        };
        _credentialsService.Setup(aks =>
                aks.AssociateMfaAuthenticatorForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CredentialMfaAuthenticatorType>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(association);

        var result =
            await _application.AssociateMfaAuthenticatorAsync(_caller.Object, "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, "aphonenumber", CancellationToken.None);

        result.Value.Should().Be(association);
        _credentialsService.Verify(aks => aks.AssociateMfaAuthenticatorForUserAsync(_caller.Object, "acallerid",
            "anmfatoken", CredentialMfaAuthenticatorType.TotpAuthenticator, "aphonenumber", CancellationToken.None));
    }

    [Fact]
    public async Task WhenConfirmMfaAuthenticatorAssociationAsync_ThenConfirms()
    {
        var confirmation = new CredentialMfaAuthenticatorConfirmation
        {
            Authenticators =
            [
                new CredentialMfaAuthenticator
                {
                    Id = "anauthenticatorid",
                    Type = CredentialMfaAuthenticatorType.TotpAuthenticator,
                    IsActive = true
                }
            ]
        };
        _credentialsService.Setup(aks =>
                aks.ConfirmMfaAuthenticatorAssociationForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CredentialMfaAuthenticatorType>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(confirmation);

        var result =
            await _application.ConfirmMfaAuthenticatorAssociationAsync(_caller.Object, "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, "anoobcode", "aconfirmationcode",
                CancellationToken.None);

        result.Value.Should().Be(confirmation);
        _credentialsService.Verify(aks => aks.ConfirmMfaAuthenticatorAssociationForUserAsync(_caller.Object,
            "acallerid",
            "anmfatoken", CredentialMfaAuthenticatorType.TotpAuthenticator, "anoobcode", "aconfirmationcode",
            CancellationToken.None));
    }

    [Fact]
    public async Task WhenChallengeMfaAuthenticatorAsync_ThenChallenges()
    {
        var challenge = new CredentialMfaAuthenticatorChallenge
        {
            Type = CredentialMfaAuthenticatorType.OobSms,
            OobCode = "anoobcode"
        };
        _credentialsService.Setup(aks =>
                aks.ChallengeMfaAuthenticatorForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        var result =
            await _application.ChallengeMfaAuthenticatorAsync(_caller.Object, "anmfatoken", "anauthenticatorid",
                CancellationToken.None);

        result.Value.Should().Be(challenge);
        _credentialsService.Verify(aks =>
            aks.ChallengeMfaAuthenticatorForUserAsync(_caller.Object, "acallerid", "anmfatoken", "anauthenticatorid",
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenVerifyMfaAuthenticatorAsync_ThenVerifies()
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
                aks.VerifyMfaAuthenticatorForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<CredentialMfaAuthenticatorType>(), It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        var result =
            await _application.VerifyMfaAuthenticatorAsync(_caller.Object, "anmfatoken",
                CredentialMfaAuthenticatorType.TotpAuthenticator, "anoobcode", "aconfirmationcode",
                CancellationToken.None);

        result.Value.Should().Be(tokens);
        _credentialsService.Verify(aks => aks.VerifyMfaAuthenticatorForUserAsync(_caller.Object, "acallerid",
            "anmfatoken",
            CredentialMfaAuthenticatorType.TotpAuthenticator, "anoobcode", "aconfirmationcode",
            CancellationToken.None));
    }

    [Fact]
    public async Task WhenResetPasswordMfaAsync_ThenResets()
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
                aks.ResetPasswordMfaForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var result =
            await _application.ResetPasswordMfaAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Value.Should().Be(credential);
        _credentialsService.Verify(aks =>
            aks.ResetPasswordMfaForUserAsync(_caller.Object, "auserid", CancellationToken.None));
    }
}