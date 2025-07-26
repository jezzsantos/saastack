using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class OAuth2ClientsApiSpec : WebApiSpec<Program>
{
    public OAuth2ClientsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenCreateClientAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var result = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenCreateClientAndAuthenticated_ThenCreatesClient()
    {
        var login = await LoginUserAsync();
        var result = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname",
            RedirectUri = "https://localhost/callback"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Client.Should().NotBeNull();
        result.Content.Value.Client.Id.Should().NotBeEmpty();
        result.Content.Value.Client.Name.Should().Be("aclientname");
        result.Content.Value.Client.RedirectUri.Should().Be("https://localhost/callback");
    }

    [Fact]
    public async Task WhenGetClient_ThenReturnsClient()
    {
        var login = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;

        var result = await Api.GetAsync(new GetOAuth2ClientRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Client.Id.Should().Be(client.Id);
        result.Content.Value.Client.Name.Should().Be("aclientname");
    }

    [Fact]
    public async Task WhenUpdateClient_ThenUpdatesClient()
    {
        var login = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;

        var result = await Api.PutAsync(new UpdateOAuth2ClientRequest
        {
            Id = client.Id,
            Name = "anotherclientname",
            RedirectUri = "https://localhost/callback"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Client.Should().NotBeNull();
        result.Content.Value.Client.Id.Should().Be(client.Id);
        result.Content.Value.Client.Name.Should().Be("anotherclientname");
        result.Content.Value.Client.RedirectUri.Should().Be("https://localhost/callback");
    }

    [Fact]
    public async Task WhenDeleteClient_ThenDeletesClient()
    {
        var login = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;

        var result = await Api.DeleteAsync(new DeleteOAuth2ClientRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var operations = await LoginUserAsync(LoginUser.Operator);
        var clients = await Api.GetAsync(new SearchAllOAuth2ClientsRequest(),
            req => req.SetJWTBearerToken(operations.AccessToken));

        clients.Content.Value.Clients.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenSearchAllClients_ThenReturnsClients()
    {
        var login = await LoginUserAsync();
        var client1 = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname1"
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;
        var client2 = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname2"
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;

        var operations = await LoginUserAsync(LoginUser.Operator);
        var clients = await Api.GetAsync(new SearchAllOAuth2ClientsRequest(),
            req => req.SetJWTBearerToken(operations.AccessToken));

        clients.Content.Value.Clients.Count.Should().Be(2);
        clients.Content.Value.Clients.Should().Contain(c => c.Id == client1.Id);
        clients.Content.Value.Clients.Should().Contain(c => c.Id == client2.Id);
    }

    [Fact]
    public async Task WhenRegenerateClientSecret_ThenGeneratesSecret()
    {
        var login = await LoginUserAsync();
        var client = (await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken))).Content.Value.Client;

        var result = await Api.PostAsync(new RegenerateOAuth2ClientSecretRequest
        {
            Id = client.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Client!.Id.Should().Be(client.Id);
        result.Content.Value.Client.Name.Should().Be("aclientname");
        result.Content.Value.Client.Secret.Should().NotBeEmpty();
        result.Content.Value.Client.ExpiresOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task WhenConsentClientForCaller_ThenConsentsToClient()
    {
        var login = await LoginUserAsync();
        var created = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.PostAsync(new ConsentToOAuth2ClientForCallerRequest
        {
            Id = created.Content.Value.Client.Id,
            Scope = "profile email",
            Consented = true
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Consented.Should().BeTrue();
    }

    [Fact]
    public async Task WhenConsentClientForCallerAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var login = await LoginUserAsync();
        var created = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.PostAsync(new ConsentToOAuth2ClientForCallerRequest
        {
            Id = created.Content.Value.Client.Id,
            Scope = "profile email",
            Consented = true
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenGetClientConsentForCaller_ThenReturnsConsentStatus()
    {
        var login = await LoginUserAsync();
        var created = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        // First consent to the client
        await Api.PostAsync(new ConsentToOAuth2ClientForCallerRequest
        {
            Id = created.Content.Value.Client.Id,
            Scope = "profile email",
            Consented = true
        }, req => req.SetJWTBearerToken(login.AccessToken));

        // Then get the consent status
        var result = await Api.GetAsync(new GetOAuth2ClientConsentForCallerRequest
        {
            Id = created.Content.Value.Client.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Consented.Should().BeTrue();
    }

    [Fact]
    public async Task WhenGetClientConsentForCallerAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var login = await LoginUserAsync();
        var created = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.GetAsync(new GetOAuth2ClientConsentForCallerRequest
        {
            Id = created.Content.Value.Client.Id
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenRevokeClientConsentForCaller_ThenRevokesConsent()
    {
        var login = await LoginUserAsync();
        var created = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        // First consent to the client
        await Api.PostAsync(new ConsentToOAuth2ClientForCallerRequest
        {
            Id = created.Content.Value.Client.Id,
            Scope = "profile email",
            Consented = true
        }, req => req.SetJWTBearerToken(login.AccessToken));

        // Then revoke the consent
        var result = await Api.DeleteAsync(new RevokeOAuth2ClientConsentForCallerRequest
        {
            Id = created.Content.Value.Client.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify consent is revoked
        var consentResult = await Api.GetAsync(new GetOAuth2ClientConsentForCallerRequest
        {
            Id = created.Content.Value.Client.Id
        }, req => req.SetJWTBearerToken(login.AccessToken));

        consentResult.Content.Value.Consented.Should().BeFalse();
    }

    [Fact]
    public async Task WhenRevokeClientConsentForCallerAndUnauthenticated_ThenReturnsUnauthorized()
    {
        var login = await LoginUserAsync();
        var created = await Api.PostAsync(new CreateOAuth2ClientRequest
        {
            Name = "aclientname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.DeleteAsync(new RevokeOAuth2ClientConsentForCallerRequest
        {
            Id = created.Content.Value.Client.Id
        });

        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}