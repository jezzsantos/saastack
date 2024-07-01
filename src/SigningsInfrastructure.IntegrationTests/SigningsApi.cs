using ApiHost1;
using Application.Resources.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Signings;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SigningsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class SigningsApi : WebApiSpec<Program>
{
    public SigningsApi(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenCreateDraft_ThenCreatedDraft()
    {
        var login = await LoginUserAsync();

        var result = await Api.PostAsync(new CreateDraftSigningRequestRequest
        {
            Signees =
            [
                new Signee
                {
                    EmailAddress = "auser@company.com",
                    PhoneNumber = "+6498876986"
                }
            ]
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.SigningRequest!.OrganizationId.Should().Be(login.DefaultOrganizationId);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //do nothing
    }
}