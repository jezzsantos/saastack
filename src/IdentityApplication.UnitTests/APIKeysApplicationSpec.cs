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
public class APIKeysApplicationSpec
{
    private readonly Mock<IIdentityServerApiKeyService> _apiKeyService;
    private readonly APIKeysApplication _application;
    private readonly Mock<ICallerContext> _caller;

    public APIKeysApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        _apiKeyService = new Mock<IIdentityServerApiKeyService>();
        var identityServerProvider = new Mock<IIdentityServerProvider>();
        identityServerProvider.Setup(p => p.ApiKeyService)
            .Returns(_apiKeyService.Object);

        _application = new APIKeysApplication(identityServerProvider.Object);
    }

    [Fact]
    public async Task WhenAuthenticateAsync_ThenAuthenticates()
    {
        _apiKeyService.Setup(aks =>
                aks.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "auserid"
            });

        var result =
            await _application.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None);

        result.Value.Id.Should().Be("auserid");
        _apiKeyService.Verify(aks => aks.AuthenticateAsync(_caller.Object, "anapikey", CancellationToken.None));
    }

    [Fact]
    public async Task WhenCreateAPIKeyForUserAsync_ThenCreates()
    {
        var expiresOn = DateTime.UtcNow.AddHours(1);
        _apiKeyService.Setup(aks => aks.CreateAPIKeyForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new APIKey
            {
                Id = "anid",
                Key = "akey",
                UserId = "auserid",
                Description = "adescription",
                ExpiresOnUtc = expiresOn
            });

        var result = await _application.CreateAPIKeyForUserAsync(_caller.Object, "auserid", "adescription", expiresOn,
            CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        result.Value.Key.Should().Be("akey");
        result.Value.UserId.Should().Be("auserid");
        result.Value.Description.Should().Be("adescription");
        result.Value.ExpiresOnUtc.Should().Be(expiresOn);
        _apiKeyService.Verify(aks =>
            aks.CreateAPIKeyForUserAsync(_caller.Object, "auserid", "adescription", expiresOn,
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenSearchAllAPIKeysAsync_ThenReturnsAll()
    {
        var results = new SearchResults<APIKey>();
        _apiKeyService.Setup(aks => aks.SearchAllAPIKeysForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<SearchOptions>(), It.IsAny<GetOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        var result = await _application.SearchAllAPIKeysForCallerAsync(_caller.Object, new SearchOptions(),
            new GetOptions(), CancellationToken.None);

        result.Value.Should().Be(results);
        _apiKeyService.Verify(aks => aks.SearchAllAPIKeysForUserAsync(_caller.Object, "acallerid",
            It.IsAny<SearchOptions>(), It.IsAny<GetOptions>(), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenDeleteAPIKeyAsync_ThenDeletes()
    {
        _apiKeyService.Setup(aks => aks.DeleteAPIKeyForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.DeleteAPIKeyAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeSuccess();
        _apiKeyService.Verify(aks =>
            aks.DeleteAPIKeyForUserAsync(_caller.Object, "anid", "acallerid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRevokeAPIKeyAsync_ThenRevokes()
    {
        _apiKeyService.Setup(aks =>
                aks.RevokeAPIKeyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _application.RevokeAPIKeyAsync(_caller.Object, "anid", CancellationToken.None);

        result.Should().BeSuccess();
        _apiKeyService.Verify(aks =>
            aks.RevokeAPIKeyAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
    }
}