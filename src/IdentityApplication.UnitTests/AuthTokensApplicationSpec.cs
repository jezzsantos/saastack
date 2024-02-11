using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class AuthTokensApplicationSpec
{
    private readonly AuthTokensApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IJWTTokensService> _jwtTokensService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IAuthTokensRepository> _repository;

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
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()))
            .Returns((AuthTokensRoot root, CancellationToken _) =>
                Task.FromResult<Result<AuthTokensRoot, Error>>(root));

        _application = new AuthTokensApplication(_recorder.Object, _idFactory.Object, _jwtTokensService.Object,
            _endUsersService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenIssueTokensAsyncAndUserNotExist_ThenReturnsTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _jwtTokensService.Setup(jts => jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>()))
            .Returns(Task.FromResult<Result<AccessTokens, Error>>(
                new AccessTokens("anaccesstoken", expiresOn, "arefreshtoken", expiresOn)));
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<AuthTokensRoot>, Error>>(Optional<AuthTokensRoot>.None));

        var result = await _application.IssueTokensAsync(_caller.Object, user, CancellationToken.None);

        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.AccessTokenExpiresOn.Should().Be(expiresOn);
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken == "anaccesstoken"
            && at.RefreshToken == "arefreshtoken"
            && at.AccessTokenExpiresOn == expiresOn
            && at.RefreshTokenExpiresOn == expiresOn
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenIssueTokensAsyncAndUserExists_ThenReturnsTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        var authTokens = AuthTokensRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId()).Value;
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _jwtTokensService.Setup(jts => jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>()))
            .Returns(Task.FromResult<Result<AccessTokens, Error>>(
                new AccessTokens("anaccesstoken", expiresOn, "arefreshtoken", expiresOn)));
        _repository.Setup(rep => rep.FindByUserIdAsync(It.IsAny<Identifier>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<AuthTokensRoot>, Error>>(authTokens.ToOptional()));

        var result = await _application.IssueTokensAsync(_caller.Object, user, CancellationToken.None);

        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.AccessTokenExpiresOn.Should().Be(expiresOn);
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken == "anaccesstoken"
            && at.RefreshToken == "arefreshtoken"
            && at.AccessTokenExpiresOn == expiresOn
            && at.RefreshTokenExpiresOn == expiresOn
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndTokensNotExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<AuthTokensRoot>, Error>>(Optional<AuthTokensRoot>.None));

        var result = await _application.RefreshTokenAsync(_caller.Object, "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>()), Times.Never);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndTokensExist_ThenReturnsRefreshedTokens()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        var expiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var expiresOn2 = DateTime.UtcNow.AddMinutes(2);
        var authTokens = AuthTokensRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId()).Value;
        authTokens.SetTokens("anaccesstoken1", "arefreshtoken1", expiresOn1, expiresOn1);
        _repository.Setup(rep => rep.FindByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<AuthTokensRoot>, Error>>(authTokens.ToOptional()));
        _endUsersService.Setup(eus =>
                eus.GetMembershipsAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<EndUserWithMemberships, Error>>(user));
        _jwtTokensService.Setup(jts => jts.IssueTokensAsync(It.IsAny<EndUserWithMemberships>()))
            .Returns(Task.FromResult<Result<AccessTokens, Error>>(
                new AccessTokens("anaccesstoken2", expiresOn2, "arefreshtoken2", expiresOn2)));

        var result = await _application.RefreshTokenAsync(_caller.Object, "arefreshtoken1", CancellationToken.None);

        result.Value.AccessToken.Value.Should().Be("anaccesstoken2");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn2);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken2");
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn2);
        _jwtTokensService.Verify(jts => jts.IssueTokensAsync(user));
        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken == "anaccesstoken2"
            && at.RefreshToken == "arefreshtoken2"
            && at.AccessTokenExpiresOn == expiresOn2
            && at.RefreshTokenExpiresOn == expiresOn2
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRevokeTokenAsyncAndTokensNotExist_ThenReturnsError()
    {
        _repository.Setup(rep => rep.FindByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<AuthTokensRoot>, Error>>(Optional<AuthTokensRoot>.None));

        var result =
            await _application.RevokeRefreshTokenAsync(_caller.Object, "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _repository.Verify(rep => rep.SaveAsync(It.IsAny<AuthTokensRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenRevokeRefreshTokenAsync_ThenRevokes()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        var authTokens = AuthTokensRoot.Create(_recorder.Object, _idFactory.Object, "auserid".ToId()).Value;
        authTokens.SetTokens("anaccesstoken", "arefreshtoken", expiresOn, expiresOn);
        _repository.Setup(rep => rep.FindByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<AuthTokensRoot>, Error>>(authTokens.ToOptional()));

        await _application.RevokeRefreshTokenAsync(_caller.Object, "arefreshtoken", CancellationToken.None);

        _repository.Verify(rep => rep.SaveAsync(It.Is<AuthTokensRoot>(at =>
            at.Id == "anid"
            && at.AccessToken == Optional<string>.None
            && at.RefreshToken == Optional<string>.None
            && at.AccessTokenExpiresOn == Optional<DateTime>.None
            && at.RefreshTokenExpiresOn == Optional<DateTime>.None
        ), It.IsAny<CancellationToken>()));
    }
}