using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class APIKeyApiSpec : WebApiSpec<Program>
{
    public APIKeyApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenCreateAPIKey_ThenCreatesNewKey()
    {
        var login = await LoginUserAsync();
        var result = await Api.PostAsync(new CreateAPIKeyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.ApiKey.Should().NotBeNullOrEmpty();

        var keys = (await Api.GetAsync(new SearchAllAPIKeysForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Keys;

        keys.Count.Should().Be(1);
        keys[0].Description.Should().Be(login.User.Id);
        keys[0].Key.Should().NotBeNullOrEmpty();
        keys[0].ExpiresOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task WhenCreateMultipleAPIKeys_ThenCreatesKeysButExpiresOlderOnes()
    {
        var login = await LoginUserAsync();
        var apiKey1 = (await Api.PostAsync(new CreateAPIKeyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.ApiKey;
        var apiKey2 = (await Api.PostAsync(new CreateAPIKeyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.ApiKey;

        var keys = (await Api.GetAsync(new SearchAllAPIKeysForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Keys;

        keys.Count.Should().Be(2);
        keys[0].Description.Should().Be(login.User.Id);
        keys[0].Key.Should().NotBeNullOrEmpty();
        keys[0].ExpiresOnUtc.Should().BeNull();
        keys[1].Description.Should().Be(login.User.Id);
        keys[1].Key.Should().NotBeNullOrEmpty();
        keys[1].ExpiresOnUtc.Should().BeNear(DateTime.UtcNow, TimeSpan.FromMinutes(1));

#if TESTINGONLY
        var authenticate1 = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey1));

        authenticate1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
#endif

#if TESTINGONLY
        var authenticate2 = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey2));

        authenticate2.StatusCode.Should().Be(HttpStatusCode.OK);
        authenticate2.Content.Value.CallerId.Should().Be(login.User.Id);
#endif
    }

    [Fact]
    public async Task WhenDeleteAPIKeyByCreator_ThenDeletes()
    {
        var login = await LoginUserAsync();
        var apiKey = (await Api.PostAsync(new CreateAPIKeyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.ApiKey;

        var keys = (await Api.GetAsync(new SearchAllAPIKeysForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Keys;

        await Api.DeleteAsync(new DeleteAPIKeyRequest
        {
            Id = keys[0].Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

#if TESTINGONLY
        var authenticate1 = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey));

        authenticate1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
#endif
    }

    [Fact]
    public async Task WhenRevokeAPIKeyByOperations_ThenRevokes()
    {
        var login = await LoginUserAsync();
        var apiKey = (await Api.PostAsync(new CreateAPIKeyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.ApiKey;

        var keys = (await Api.GetAsync(new SearchAllAPIKeysForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Keys;

        var @operator = await LoginUserAsync(LoginUser.Operator);
        await Api.DeleteAsync(new RevokeAPIKeyRequest
        {
            Id = keys[0].Id
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

#if TESTINGONLY
        var authenticate1 = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey));

        authenticate1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
#endif
    }

    [Fact]
    public async Task WhenCallingSecureApiWithNewApiKey_ThenAuthenticates()
    {
        var login = await LoginUserAsync();
        var apiKey = (await Api.PostAsync(new CreateAPIKeyRequest(),
            req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.ApiKey;

#if TESTINGONLY
        var result = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(login.User.Id);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}