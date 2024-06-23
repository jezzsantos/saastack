#if TESTINGONLY
using System.Net;
using Application.Resources.Shared;
using Common.Configuration;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Services.Shared;
using FluentAssertions;
using IdentityInfrastructure.ApplicationServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class AuthZApiSpec : WebApiSpec<ApiHost1.Program>
{
    private readonly IConfigurationSettings _settings;
    private readonly ITokensService _tokensService;

    public AuthZApiSpec(WebApiSetup<ApiHost1.Program> setup) : base(setup)
    {
        EmptyAllRepositories();
        _settings = setup.GetRequiredService<IConfigurationSettings>();
        _tokensService = setup.GetRequiredService<ITokensService>();
    }

    [Fact]
    public async Task WhenAuthorizeByNothingAndAnonymous_ThenReturns200()
    {
        var result = await Api.GetAsync(new AuthorizeByNothingTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(CallerConstants.AnonymousUserId);
    }

    [Fact]
    public async Task WhenAuthorizeByNothingAndNoRolesOrFeatures_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            new List<RoleLevel>(), new List<FeatureLevel>());

        var result = await Api.GetAsync(new AuthorizeByNothingTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByNothingAndAnyRoles_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            new List<RoleLevel> { PlatformRoles.Standard, PlatformRoles.Operations }, new List<FeatureLevel>());

        var result = await Api.GetAsync(new AuthorizeByNothingTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByNothingAndAnyFeatures_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            new List<RoleLevel>(),
            new List<FeatureLevel> { PlatformFeatures.PaidTrial, PlatformFeatures.Basic });

        var result = await Api.GetAsync(new AuthorizeByNothingTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByRoleRequestWithNoToken_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthorizeByRoleTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByRoleRequestWithNoRole_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, new List<RoleLevel>(), new List<FeatureLevel>());

        var result = await Api.GetAsync(new AuthorizeByRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByRoleRequestWithWrongRole_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, new List<RoleLevel> { new("awrongrole") },
            new List<FeatureLevel>());

        var result = await Api.GetAsync(new AuthorizeByRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByRoleRequestIncludingCorrectRole_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService,
            new List<RoleLevel> { PlatformRoles.Standard, PlatformRoles.Operations }, new List<FeatureLevel>
            {
                PlatformFeatures.Basic // Must have at least this feature by default
            });

        var result = await Api.GetAsync(new AuthorizeByRoleTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    [Fact]
    public async Task WhenAuthorizeByFeatureRequestWithNoToken_ThenReturns401()
    {
        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Content.Error.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task WhenAuthorizeByFeatureRequestWithNoFeature_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, new List<RoleLevel>(), new List<FeatureLevel>());

        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByFeatureRequestWithWrongFeature_ThenReturns403()
    {
        var token = CreateJwtToken(_settings, _tokensService, new List<RoleLevel>
            {
                PlatformRoles.Standard
            },
            new List<FeatureLevel> { new("awrongfeature") });

        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Content.Error.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenAuthorizeByFeatureRequestIncludingCorrectFeature_ThenReturns200()
    {
        var token = CreateJwtToken(_settings, _tokensService, new List<RoleLevel>
            {
                PlatformRoles.Standard
            },
            new List<FeatureLevel>
            {
                PlatformFeatures.PaidTrial
            });

        var result = await Api.GetAsync(new AuthorizeByFeatureTestingOnlyRequest(),
            req => req.SetJWTBearerToken(token));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be("auserid");
    }

    private static string CreateJwtToken(IConfigurationSettings settings, ITokensService tokensService,
        List<RoleLevel> platFormRoles, List<FeatureLevel> platformFeatures)
    {
        return new JWTTokensService(settings, tokensService)
            .IssueTokensAsync(new EndUserWithMemberships
            {
                Id = "auserid",
                Roles = platFormRoles.Select(rol => rol.Name).ToList(),
                Features = platformFeatures.Select(rol => rol.Name).ToList()
            }).GetAwaiter().GetResult().Value.AccessToken;
    }
}
#endif