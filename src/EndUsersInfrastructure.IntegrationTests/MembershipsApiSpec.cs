using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EndUsersInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class MembershipsApiSpec : WebApiSpec<Program>
{
    public MembershipsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenChangeDefaultOrganization_ThenChangesDefault()
    {
        var login = await LoginUserAsync();

        var organizationId1 = login.DefaultOrganizationId!;
        var organization2 = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var organizationId2 = organization2.Content.Value.Organization!.Id;
        login = await ReAuthenticateUserAsync(login);
        login.DefaultOrganizationId.Should().Be(organizationId2);

        var result = await Api.PutAsync(new ChangeDefaultOrganizationRequest
        {
            OrganizationId = organizationId1
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        login = await ReAuthenticateUserAsync(login);
        login.DefaultOrganizationId.Should().Be(organizationId1);
    }

    [Fact]
    public async Task WhenListMembershipsForCaller_ThenReturnsMemberships()
    {
        var login = await LoginUserAsync();

        var result = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var organizationId = result.Content.Value.Organization!.Id;
        result.Content.Value.Organization!.CreatedById.Should().Be(login.User.Id);
        result.Content.Value.Organization!.Name.Should().Be("aname");
        result.Content.Value.Organization!.Ownership.Should().Be(OrganizationOwnership.Shared);

        login = await ReAuthenticateUserAsync(login);
        login.DefaultOrganizationId.Should().Be(organizationId);

        var memberships = await Api.GetAsync(new ListMembershipsForCallerRequest(),
            req => req.SetJWTBearerToken(login.AccessToken));

        memberships.Content.Value.Memberships!.Count.Should().Be(2);
        memberships.Content.Value.Memberships![0].OrganizationId.Should().NotBeNull();
        memberships.Content.Value.Memberships![0].Ownership.Should().Be(OrganizationOwnership.Personal);
        memberships.Content.Value.Memberships![1].OrganizationId.Should().Be(organizationId);
        memberships.Content.Value.Memberships![1].Ownership.Should().Be(OrganizationOwnership.Shared);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        // Override dependencies here
    }
}