using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using FluentAssertions;
using IdentityApplication.Persistence;
using IdentityDomain;
using Microsoft.IdentityModel.Tokens;
using Moq;
using UnitTesting.Common;
using Xunit;
using AuthToken = IdentityDomain.AuthToken;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class AuthTokensApplicationSpec
{
    private readonly string _accessToken1;
    private readonly string _accessToken2;
    private readonly AccessTokens _allTokens1;
    private readonly AccessTokens _allTokens2;
    private readonly AuthTokensApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly DateTime _expiresOn1;
    private readonly DateTime _expiresOn2;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly string _idToken1;
    private readonly string _idToken2;
    private readonly Mock<IJWTTokensService> _jwtTokensService;
    private readonly Mock<IRecorder> _recorder;
    private readonly string _refreshToken1;
    private readonly string _refreshToken2;
    private readonly Mock<IAuthTokensRepository> _repository;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<IUserProfilesService> _userProfilesService;

    public AuthTokensApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        _jwtTokensService = new Mock<IJWTTokensService>();
        _repository = new Mock<IAuthTokensRepository>();
        _endUsersService = new Mock<IEndUsersService>();
        _userProfilesService = new Mock<IUserProfilesService>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()))
            .Returns((AuthTokensRoot root, CancellationToken _) =>
                Task.FromResult<Result<AuthTokensRoot, Error>>(root));
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns((string encrypted) => encrypted.StartsWith("encrypted_")
                ? encrypted.Substring(10)
                : encrypted);
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns((string plain) => "encrypted_" + plain);
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateTokenDigest(It.IsAny<string>()))
            .Returns("adigestvalue");

        _accessToken1 = CreateJwtToken("access1");
        _refreshToken1 = CreateJwtToken("refresh1");
        _idToken1 = CreateJwtToken("id1");
        _expiresOn1 = DateTime.UtcNow.AddMinutes(1);
        _allTokens1 = new AccessTokens(_accessToken1, _expiresOn1, _refreshToken1, _expiresOn1, _idToken1, _expiresOn1);
        _accessToken1 = CreateJwtToken("access");
        _accessToken2 = CreateJwtToken("access1");
        _refreshToken2 = CreateJwtToken("refresh1");
        _idToken2 = CreateJwtToken("id1");
        _expiresOn2 = DateTime.UtcNow.AddMinutes(2);
        _allTokens2 = new AccessTokens(_accessToken2, _expiresOn2, _refreshToken2, _expiresOn2, _idToken2, _expiresOn2);

        _application = new AuthTokensApplication(_recorder.Object, _idFactory.Object, _encryptionService.Object,
            _tokensService.Object, _jwtTokensService.Object, _endUsersService.Object, _userProfilesService.Object,
            _repository.Object);
    }

    [Fact]
    public async Task WhenIssueTokensAsyncAndUserNotExist_ThenReturnsTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var profile = new UserProfile
        {
            Id = "aprofileid",
            UserId = "auserid",
            Name = new PersonName
            {
                FirstName = "afirstname",
                LastName = "alastname"
            },
            DisplayName = "adisplayname"
        };
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        _jwtTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(_allTokens1);
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<AuthTokensRoot>.None);
        var additionalData = new Dictionary<string, object>();

        var result =
            await _application.IssueTokensAsync(_caller.Object, "auserid", ["ascope"], additionalData,
                CancellationToken.None);

        result.Value.AccessToken.Value.Should().StartWith("eyJ");
        result.Value.AccessToken.ExpiresOn.Should().Be(_expiresOn1);
        result.Value.RefreshToken.Value.Should().StartWith("eyJ");
        result.Value.RefreshToken.ExpiresOn.Should().Be(_expiresOn1);
        result.Value.IdToken!.Value.Should().StartWith("eyJ");
        result.Value.IdToken.ExpiresOn.Should().Be(_expiresOn1);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(
                It.Is<ICallerContext>(cc => cc.CallerId == CallerConstants.MaintenanceAccountUserId), "anid",
                It.IsAny<CancellationToken>()));
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user, profile, It.Is<IReadOnlyList<string>>(scopes =>
            scopes.SequenceEqual(new List<string> { "ascope" })), additionalData));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken.Value.StartsWith("encrypted_ey")
            && at.RefreshToken.Value.StartsWith("encrypted_ey")
            && at.RefreshTokenDigest == "adigestvalue"
            && at.IdToken.Value.StartsWith("encrypted_ey")
            && at.AccessTokenExpiresOn == _expiresOn1
            && at.RefreshTokenExpiresOn == _expiresOn1
            && at.IdTokenExpiresOn == _expiresOn1
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenIssueTokensAsyncWithoutScopes_ThenReturnsTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var authTokens = AuthTokensRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;

        _jwtTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(_allTokens1);
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authTokens.ToOptional());
        var additionalData = new Dictionary<string, object>();

        var result =
            await _application.IssueTokensAsync(_caller.Object, "auserid", null, additionalData,
                CancellationToken.None);

        result.Value.AccessToken.Value.Should().StartWith("eyJ");
        result.Value.AccessToken.ExpiresOn.Should().Be(_expiresOn1);
        result.Value.RefreshToken.Value.Should().StartWith("eyJ");
        result.Value.RefreshToken.ExpiresOn.Should().Be(_expiresOn1);
        result.Value.IdToken!.Value.Should().StartWith("eyJ");
        result.Value.IdToken.ExpiresOn.Should().Be(_expiresOn1);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user, null, null, additionalData));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken.Value.StartsWith("encrypted_ey")
            && at.RefreshToken.Value.StartsWith("encrypted_ey")
            && at.RefreshTokenDigest == "adigestvalue"
            && at.IdToken.Value.StartsWith("encrypted_ey")
            && at.AccessTokenExpiresOn == _expiresOn1
            && at.RefreshTokenExpiresOn == _expiresOn1
            && at.IdTokenExpiresOn == _expiresOn1
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenIssueTokensAsyncWithScopes_ThenReturnsTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var profile = new UserProfile
        {
            Id = "aprofileid",
            UserId = "auserid",
            Name = new PersonName
            {
                FirstName = "afirstname",
                LastName = "alastname"
            },
            DisplayName = "adisplayname"
        };
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var authTokens = AuthTokensRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;

        _jwtTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(_allTokens1);
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authTokens.ToOptional());
        var additionalData = new Dictionary<string, object>();

        var result =
            await _application.IssueTokensAsync(_caller.Object, "auserid", ["ascope"], additionalData,
                CancellationToken.None);

        result.Value.AccessToken.Value.Should().StartWith("eyJ");
        result.Value.AccessToken.ExpiresOn.Should().Be(_expiresOn1);
        result.Value.RefreshToken.Value.Should().StartWith("eyJ");
        result.Value.RefreshToken.ExpiresOn.Should().Be(_expiresOn1);
        result.Value.IdToken!.Value.Should().StartWith("eyJ");
        result.Value.IdToken.ExpiresOn.Should().Be(_expiresOn1);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(
                It.Is<ICallerContext>(cc => cc.CallerId == CallerConstants.MaintenanceAccountUserId), "anid",
                It.IsAny<CancellationToken>()));
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user, profile, It.Is<IReadOnlyList<string>>(scopes =>
            scopes.SequenceEqual(new List<string> { "ascope" })), additionalData));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken.Value.StartsWith("encrypted_ey")
            && at.RefreshToken.Value.StartsWith("encrypted_ey")
            && at.RefreshTokenDigest == "adigestvalue"
            && at.IdToken.Value.StartsWith("encrypted_ey")
            && at.AccessTokenExpiresOn == _expiresOn1
            && at.RefreshTokenExpiresOn == _expiresOn1
            && at.IdTokenExpiresOn == _expiresOn1
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndRefreshTokenNotExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<AuthTokensRoot>.None);

        var result =
            await _application.RefreshTokenAsync(_caller.Object, "eyarefreshtoken", null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _tokensService.Verify(ts => ts.CreateTokenDigest("eyarefreshtoken"));
        _jwtTokensService.Verify(
            jts => jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()), Times.Never);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndNotAPerson_ThenReturnsError()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid",
            Classification = EndUserClassification.Machine
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var authTokens = AuthTokensRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;
        authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "encrypted_" + _accessToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "encrypted_" + _refreshToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "encrypted_" + _idToken1, _expiresOn1).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authTokens.ToOptional());

        var result =
            await _application.RefreshTokenAsync(_caller.Object, _refreshToken1, null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _tokensService.Verify(ts => ts.CreateTokenDigest(_refreshToken1));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), "auserid", It.IsAny<CancellationToken>()),
            Times.Never);
        _jwtTokensService.Verify(
            jts => jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()), Times.Never);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndSuspended_ThenReturnsError()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid",
            Classification = EndUserClassification.Person,
            Access = EndUserAccess.Suspended
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var authTokens = AuthTokensRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;
        authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "encrypted_" + _accessToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "encrypted_" + _refreshToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "encrypted_" + _idToken1, _expiresOn1).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authTokens.ToOptional());

        var result =
            await _application.RefreshTokenAsync(_caller.Object, _refreshToken1, null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityLocked, Resources.AuthTokensApplication_AccountSuspended);
        _tokensService.Verify(ts => ts.CreateTokenDigest(_refreshToken1));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), "auserid", It.IsAny<CancellationToken>()),
            Times.Never);
        _jwtTokensService.Verify(
            jts => jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()), Times.Never);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncWithoutScopes_ThenReturnsRefreshedTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var authTokens = AuthTokensRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;
        authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "encrypted_" + _accessToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "encrypted_" + _refreshToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "encrypted_" + _idToken1, _expiresOn1).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authTokens.ToOptional());
        _jwtTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(_allTokens2);
        var additionalData = new Dictionary<string, object>();

        var result = await _application.RefreshTokenAsync(_caller.Object, _refreshToken1, null, additionalData,
            CancellationToken.None);

        result.Value.AccessToken.Value.Should().Be(_accessToken2);
        result.Value.AccessToken.ExpiresOn.Should().Be(_expiresOn2);
        result.Value.RefreshToken.Value.Should().Be(_refreshToken2);
        result.Value.RefreshToken.ExpiresOn.Should().Be(_expiresOn2);
        result.Value.IdToken!.Value.Should().Be(_idToken2);
        result.Value.IdToken.ExpiresOn.Should().Be(_expiresOn2);
        _tokensService.Verify(ts => ts.CreateTokenDigest(_refreshToken1));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(
            ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user, null, null, additionalData));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken == "encrypted_" + _accessToken2
            && at.RefreshToken == "encrypted_" + _refreshToken2
            && at.RefreshTokenDigest == "adigestvalue"
            && at.IdToken == "encrypted_" + _idToken2
            && at.AccessTokenExpiresOn == _expiresOn2
            && at.RefreshTokenExpiresOn == _expiresOn2
            && at.IdTokenExpiresOn == _expiresOn2
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncWithScopes_ThenReturnsRefreshedTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var profile = new UserProfile
        {
            Id = "aprofileid",
            UserId = "auserid",
            Name = new PersonName
            {
                FirstName = "afirstname",
                LastName = "alastname"
            },
            DisplayName = "adisplayname"
        };
        _userProfilesService.Setup(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var authTokens = AuthTokensRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;
        authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "encrypted_" + _accessToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "encrypted_" + _refreshToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "encrypted_" + _idToken1, _expiresOn1).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authTokens.ToOptional());
        _jwtTokensService.Setup(jts =>
                jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile?>(),
                    It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>?>()))
            .ReturnsAsync(_allTokens2);
        var additionalData = new Dictionary<string, object>();

        var result = await _application.RefreshTokenAsync(_caller.Object, _refreshToken1, ["ascope"], additionalData,
            CancellationToken.None);

        result.Value.AccessToken.Value.Should().Be(_accessToken2);
        result.Value.AccessToken.ExpiresOn.Should().Be(_expiresOn2);
        result.Value.RefreshToken.Value.Should().Be(_refreshToken2);
        result.Value.RefreshToken.ExpiresOn.Should().Be(_expiresOn2);
        result.Value.IdToken!.Value.Should().Be(_idToken2);
        result.Value.IdToken.ExpiresOn.Should().Be(_expiresOn2);
        _tokensService.Verify(ts => ts.CreateTokenDigest(_refreshToken1));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(
                It.Is<ICallerContext>(cc => cc.CallerId == CallerConstants.MaintenanceAccountUserId), "anid",
                It.IsAny<CancellationToken>()));
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user, profile, It.Is<IReadOnlyList<string>>(scopes =>
            scopes.SequenceEqual(new List<string> { "ascope" })), additionalData));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken == "encrypted_" + _accessToken2
            && at.RefreshToken == "encrypted_" + _refreshToken2
            && at.RefreshTokenDigest == "adigestvalue"
            && at.IdToken == "encrypted_" + _idToken2
            && at.AccessTokenExpiresOn == _expiresOn2
            && at.RefreshTokenExpiresOn == _expiresOn2
            && at.IdTokenExpiresOn == _expiresOn2
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRevokeTokenAsyncAndTokensNotExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<AuthTokensRoot>.None);

        var result =
            await _application.RevokeRefreshTokenAsync(_caller.Object, "eyarefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _tokensService.Verify(ts => ts.CreateTokenDigest("eyarefreshtoken"));
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenRevokeRefreshTokenAsync_ThenRevokes()
    {
        var authTokens = AuthTokensRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;
        authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "encrypted_" + _accessToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "encrypted_" + _refreshToken1, _expiresOn1).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "encrypted_" + _idToken1, _expiresOn1).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authTokens.ToOptional());

        await _application.RevokeRefreshTokenAsync(_caller.Object, _refreshToken1, CancellationToken.None);

        _tokensService.Verify(ts => ts.CreateTokenDigest(_refreshToken1));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken == Optional<string>.None
            && at.RefreshToken == Optional<string>.None
            && at.RefreshTokenDigest == Optional<string>.None
            && at.IdToken == Optional<string>.None
            && at.AccessTokenExpiresOn == Optional<DateTime>.None
            && at.RefreshTokenExpiresOn == Optional<DateTime>.None
            && at.IdTokenExpiresOn == Optional<DateTime>.None
        ), It.IsAny<CancellationToken>()));
    }

    private static string CreateJwtToken(string subject = "test")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("a-secret-key-for-testing-purposes-only-that-is-long-enough");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", subject)]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}