using ApiHost1;
using Application.Resources.Shared;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace OrganizationsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class OrganizationsApiSpec : WebApiSpec<Program>
{
    private static int _invitationCount;

    public OrganizationsApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenGetOrganization_ThenReturnsOrganization()
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
        var login = await LoginUserAsync();

        var result = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "anorganizationname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Organization!.CreatedById.Should().Be(login.User.Id);
        result.Content.Value.Organization!.Name.Should().Be("anorganizationname");
        result.Content.Value.Organization!.Ownership.Should().Be(OrganizationOwnership.Shared);
    }

    [Fact]
    public async Task WhenInviteMembersToOrganization_ThenAddsMembers()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);
        var loginC = CreateRandomEmailAddress();

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "anorganizationname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;
        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginB.User.Profile!.EmailAddress
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginC
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var machine = await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId,
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        members.Content.Value.Members!.Count.Should().Be(4);
        members.Content.Value.Members[0].IsDefault.Should().BeTrue();
        members.Content.Value.Members[0].IsOwner.Should().BeTrue();
        members.Content.Value.Members[0].IsRegistered.Should().BeTrue();
        members.Content.Value.Members[0].UserId.Should().Be(loginA.User.Id);
        members.Content.Value.Members[0].EmailAddress.Should().Be(loginA.User.Profile!.EmailAddress);
        members.Content.Value.Members[0].Name.FirstName.Should().Be("persona");
        members.Content.Value.Members[0].Name.LastName.Should().Be("alastname");
        members.Content.Value.Members[0].Classification.Should().Be(UserProfileClassification.Person);
        members.Content.Value.Members[0].Roles.Should().ContainInOrder(TenantRoles.BillingAdmin.Name,
            TenantRoles.Owner.Name, TenantRoles.Member.Name);
        members.Content.Value.Members[1].IsDefault.Should().BeTrue();
        members.Content.Value.Members[1].IsOwner.Should().BeFalse();
        members.Content.Value.Members[1].IsRegistered.Should().BeTrue();
        members.Content.Value.Members[1].UserId.Should().Be(loginB.User.Id);
        members.Content.Value.Members[1].EmailAddress.Should().Be(loginB.User.Profile!.EmailAddress);
        members.Content.Value.Members[1].Name.FirstName.Should().Be("personb");
        members.Content.Value.Members[1].Name.LastName.Should().Be("alastname");
        members.Content.Value.Members[1].Classification.Should().Be(UserProfileClassification.Person);
        members.Content.Value.Members[1].Roles.Should().ContainSingle(role => role == TenantRoles.Member.Name);
        members.Content.Value.Members[2].IsDefault.Should().BeTrue();
        members.Content.Value.Members[2].IsOwner.Should().BeFalse();
        members.Content.Value.Members[2].IsRegistered.Should().BeFalse();
        members.Content.Value.Members[2].UserId.Should().NotBeNullOrEmpty();
        members.Content.Value.Members[2].EmailAddress.Should().Be(loginC);
        members.Content.Value.Members[2].Name.FirstName.Should().Be(loginC);
        members.Content.Value.Members[2].Name.LastName.Should().BeNull();
        members.Content.Value.Members[2].Classification.Should().Be(UserProfileClassification.Person);
        members.Content.Value.Members[2].Roles.Should().ContainSingle(role => role == TenantRoles.Member.Name);
        members.Content.Value.Members[3].IsDefault.Should().BeTrue();
        members.Content.Value.Members[3].IsOwner.Should().BeFalse();
        members.Content.Value.Members[3].IsRegistered.Should().BeTrue();
        members.Content.Value.Members[3].UserId.Should().Be(machine.Content.Value.Machine!.Id);
        members.Content.Value.Members[3].EmailAddress.Should().BeNull();
        members.Content.Value.Members[3].Name.FirstName.Should().Be("amachinename");
        members.Content.Value.Members[3].Name.LastName.Should().BeNull();
        members.Content.Value.Members[3].Classification.Should().Be(UserProfileClassification.Machine);
        members.Content.Value.Members[3].Roles.Should().ContainSingle(role => role == TenantRoles.Member.Name);
    }

    private static string CreateRandomEmailAddress()
    {
        return $"aninvitee{++_invitationCount}@company.com";
    }
}