using ApiHost1;
using Application.Resources.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace OrganizationsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class OrganizationsApiSpec : WebApiSpec<Program>
{
    public OrganizationsApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenGetDefaultOrganization_ThenReturnsOrganization()
    {
        var login = await LoginUserAsync();

        var result = await Api.GetAsync(new GetOrganizationRequest
        {
            Id = login.User.Profile?.DefaultOrganizationId!
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Organization!.CreatedById.Should().Be(login.User.Id);
        result.Content.Value.Organization!.Name.Should().Be("persona alastname");
        result.Content.Value.Organization!.Ownership.Should().Be(OrganizationOwnership.Personal);
    }

    [Fact]
    public async Task WhenCreateOrganization_ThenReturnsOrganization()
    {
        var login = await LoginUserAsync(LoginUser.Operator);

        var result = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "anorganizationname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Organization!.CreatedById.Should().Be(login.User.Id);
        result.Content.Value.Organization!.Name.Should().Be("anorganizationname");
        result.Content.Value.Organization!.Ownership.Should().Be(OrganizationOwnership.Shared);
    }
}