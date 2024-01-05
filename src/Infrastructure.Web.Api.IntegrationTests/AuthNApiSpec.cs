#if TESTINGONLY
using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Common.Configuration;
using Domain.Services.Shared.DomainServices;
using FluentAssertions;
using IdentityInfrastructure.ApplicationServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class AuthNApiSpec : WebApiSpec<Program>
{
    private readonly IConfigurationSettings _settings;
    private readonly ITokensService _tokensService;

    public AuthNApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        _settings = setup.GetRequiredService<IConfigurationSettings>();
        _tokensService = setup.GetRequiredService<ITokensService>();
    }

    [Fact]
    public async Task WhenGetHMACRequestWithNoHMACSignature_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthNHMACTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenGetHMACRequestWithWrongSignature_ThenReturns401()
    {
        var request = new AuthNHMACTestingOnlyRequest();
        var result = await Api.GetAsync(request, req => req.SetHmacAuth(request, "awrongsecret"));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenGetHMACRequestWithSignature_ThenReturnsSuccess()
    {
        var request = new AuthNHMACTestingOnlyRequest();
        var result = await Api.GetAsync(request, req => req.SetHmacAuth(request, "asecret"));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetTokenRequestWithNoBearer_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthNTokenTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenGetTokenRequestWithWrongToken_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthNTokenTestingOnlyRequest(), req => req.SetBearerToken("awrongtoken"));

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenGetTokenRequestWithToken_ThenReturnsSuccess()
    {
        var token = CreateJwtToken(_settings, _tokensService);

        var result = await Api.GetAsync(new AuthNTokenTestingOnlyRequest(), req => req.SetBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    private static string CreateJwtToken(IConfigurationSettings settings, ITokensService tokensService)
    {
        return new JWTTokensService(settings, tokensService)
            .IssueTokensAsync(new EndUser
            {
                Id = "auserid"
            }).GetAwaiter().GetResult().Value.AccessToken;
    }
}
#endif