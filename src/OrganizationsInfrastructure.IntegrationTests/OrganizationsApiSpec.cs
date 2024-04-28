using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using Infrastructure.Web.Interfaces.Clients;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace OrganizationsInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
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
            Name = "aname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var organizationId = result.Content.Value.Organization!.Id;
        result.Content.Value.Organization!.CreatedById.Should().Be(login.User.Id);
        result.Content.Value.Organization!.Name.Should().Be("aname");
        result.Content.Value.Organization!.Ownership.Should().Be(OrganizationOwnership.Shared);

        login = await ReAuthenticateUserAsync(login.User);
        login.User.Profile!.DefaultOrganizationId.Should().Be(organizationId);

        var members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        members.Content.Value.Members!.Count.Should().Be(1);
        members.Content.Value.Members[0].IsDefault.Should().BeTrue();
        members.Content.Value.Members[0].IsOwner.Should().BeTrue();
        members.Content.Value.Members[0].IsRegistered.Should().BeTrue();
        members.Content.Value.Members[0].UserId.Should().Be(login.User.Id);
        members.Content.Value.Members[0].EmailAddress.Should().Be(login.User.Profile!.EmailAddress);
        members.Content.Value.Members[0].Name.FirstName.Should().Be("persona");
        members.Content.Value.Members[0].Name.LastName.Should().Be("alastname");
        members.Content.Value.Members[0].Classification.Should().Be(UserProfileClassification.Person);
        members.Content.Value.Members[0].Roles.Should().ContainInOrder(TenantRoles.BillingAdmin.Name,
            TenantRoles.Owner.Name, TenantRoles.Member.Name);
    }

    [Fact]
    public async Task WhenInviteMembersToOrganization_ThenAddsMembers()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);
        var loginC = CreateRandomEmailAddress();

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
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

        //Automatically adds the machine to loginA organization
        var machine = await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
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
        members.Content.Value.Members[1].Roles.Should().OnlyContain(role => role == TenantRoles.Member.Name);
        members.Content.Value.Members[2].IsDefault.Should().BeTrue();
        members.Content.Value.Members[2].IsOwner.Should().BeFalse();
        members.Content.Value.Members[2].IsRegistered.Should().BeFalse();
        members.Content.Value.Members[2].UserId.Should().NotBeNullOrEmpty();
        members.Content.Value.Members[2].EmailAddress.Should().Be(loginC);
        members.Content.Value.Members[2].Name.FirstName.Should().Be(loginC);
        members.Content.Value.Members[2].Name.LastName.Should().BeNull();
        members.Content.Value.Members[2].Classification.Should().Be(UserProfileClassification.Person);
        members.Content.Value.Members[2].Roles.Should().OnlyContain(role => role == TenantRoles.Member.Name);
        members.Content.Value.Members[3].IsDefault.Should().BeTrue();
        members.Content.Value.Members[3].IsOwner.Should().BeFalse();
        members.Content.Value.Members[3].IsRegistered.Should().BeTrue();
        members.Content.Value.Members[3].UserId.Should().Be(machine.Content.Value.Machine!.Id);
        members.Content.Value.Members[3].EmailAddress.Should().BeNull();
        members.Content.Value.Members[3].Name.FirstName.Should().Be("amachinename");
        members.Content.Value.Members[3].Name.LastName.Should().BeNull();
        members.Content.Value.Members[3].Classification.Should().Be(UserProfileClassification.Machine);
        members.Content.Value.Members[3].Roles.Should().OnlyContain(role => role == TenantRoles.Member.Name);
    }

    [Fact]
    public async Task WhenChangeAvatarByNonMember_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var organizationId = loginA.User.Profile!.DefaultOrganizationId!;
        var result = await Api.PutAsync(new ChangeOrganizationAvatarRequest
            {
                Id = organizationId
            }, new PostFile(GetTestImage(), HttpContentTypes.ImagePng, "afilename"),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WhenChangeAvatarByOrgMember_ThenForbidden()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;
        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginB.User.Profile!.EmailAddress
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var result = await Api.PutAsync(new ChangeOrganizationAvatarRequest
            {
                Id = organizationId
            }, new PostFile(GetTestImage(), HttpContentTypes.ImagePng, "afilename"),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WhenChangeAvatar_ThenChanges()
    {
        var login = await LoginUserAsync();

        var organizationId = login.User.Profile!.DefaultOrganizationId!;
        var result = await Api.PutAsync(new ChangeOrganizationAvatarRequest
            {
                Id = organizationId
            }, new PostFile(GetTestImage(), HttpContentTypes.ImagePng, "afilename"),
            req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Organization!.AvatarUrl.Should().StartWith("https://localhost:5001/images/image_");
    }

    [Fact]
    public async Task WhenDeleteAvatar_ThenDeletes()
    {
        var login = await LoginUserAsync();

        var organizationId = login.User.Profile!.DefaultOrganizationId!;
        await Api.PutAsync(new ChangeOrganizationAvatarRequest
            {
                Id = organizationId
            }, new PostFile(GetTestImage(), HttpContentTypes.ImagePng, "afilename"),
            req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.DeleteAsync(new DeleteOrganizationAvatarRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Organization!.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task WhenChangeDetails_ThenDeletes()
    {
        var login = await LoginUserAsync();

        var organizationId = login.User.Profile!.DefaultOrganizationId!;
        var result = await Api.PutAsync(new ChangeOrganizationRequest
        {
            Id = organizationId,
            Name = "anewname"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Organization!.Name.Should().Be("anewname");
    }

    [Fact]
    public async Task WhenUnInviteGuestFromOrganization_ThenRemovesMember()
    {
        var loginA = await LoginUserAsync();
        var loginC = CreateRandomEmailAddress();

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;

        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginC
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));
        var loginCId = members.Content.Value.Members![1].UserId;

        await Api.DeleteAsync(new UnInviteMemberFromOrganizationRequest
        {
            Id = organizationId,
            UserId = loginCId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        members.Content.Value.Members!.Count.Should().Be(1);
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
    }

    [Fact]
    public async Task WhenUnInviteRegisteredUserFromOrganization_ThenRemovesMember()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;

        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginB.User.Profile!.EmailAddress
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));
        var loginBId = members.Content.Value.Members![1].UserId;

        await Api.DeleteAsync(new UnInviteMemberFromOrganizationRequest
        {
            Id = organizationId,
            UserId = loginBId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        members.Content.Value.Members!.Count.Should().Be(1);
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

        loginB = await ReAuthenticateUserAsync(loginB.User);
        var memberships = await Api.GetAsync(new ListMembershipsForCallerRequest(),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        memberships.Content.Value.Memberships!.Count.Should().Be(1);
        memberships.Content.Value.Memberships![0].OrganizationId.Should().NotBeNull();
        memberships.Content.Value.Memberships![0].Ownership.Should().Be(OrganizationOwnership.Personal);
    }

    [Fact]
    public async Task WhenUnInviteMembersFromOrganization_ThenRemovesMembers()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);
        var loginC = CreateRandomEmailAddress();

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
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

        //Automatically adds the machine to loginA organization
        await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));
        var loginBId = members.Content.Value.Members![1].UserId;
        var loginCId = members.Content.Value.Members![2].UserId;
        var machineId = members.Content.Value.Members![3].UserId;

        await Api.DeleteAsync(new UnInviteMemberFromOrganizationRequest
        {
            Id = organizationId,
            UserId = loginBId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));
        await Api.DeleteAsync(new UnInviteMemberFromOrganizationRequest
        {
            Id = organizationId,
            UserId = loginCId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));
        await Api.DeleteAsync(new UnInviteMemberFromOrganizationRequest
        {
            Id = organizationId,
            UserId = machineId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        members = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        members.Content.Value.Members!.Count.Should().Be(1);
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
    }

    [Fact]
    public async Task WhenAssignRoles_ThenAssigns()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;
        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginB.User.Profile!.EmailAddress
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        await Api.PutAsync(new AssignRolesToOrganizationRequest
        {
            Id = organizationId,
            UserId = loginB.User.Id,
            Roles = [TenantRoles.Owner.Name]
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var memberships = await Api.GetAsync(new ListMembershipsForCallerRequest(),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        memberships.Content.Value.Memberships!.Count.Should().Be(2);
        memberships.Content.Value.Memberships![0].OrganizationId.Should().NotBeNull();
        memberships.Content.Value.Memberships![0].Ownership.Should().Be(OrganizationOwnership.Personal);
        memberships.Content.Value.Memberships![0].Roles.Should().ContainInOrder(TenantRoles.Owner.Name);
        memberships.Content.Value.Memberships![1].OrganizationId.Should().Be(organizationId);
        memberships.Content.Value.Memberships![1].Ownership.Should().Be(OrganizationOwnership.Shared);
        memberships.Content.Value.Memberships![1].Roles.Should()
            .ContainInOrder(TenantRoles.Owner.Name, TenantRoles.Member.Name);
    }

    [Fact]
    public async Task WhenUnassignAssignedRole_ThenUnassigns()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;
        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginB.User.Profile!.EmailAddress
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        await Api.PutAsync(new AssignRolesToOrganizationRequest
        {
            Id = organizationId,
            UserId = loginB.User.Id,
            Roles = [TenantRoles.Owner.Name]
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        await Api.PutAsync(new UnassignRolesFromOrganizationRequest
        {
            Id = organizationId,
            UserId = loginB.User.Id,
            Roles = [TenantRoles.Owner.Name]
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var memberships = await Api.GetAsync(new ListMembershipsForCallerRequest(),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        memberships.Content.Value.Memberships!.Count.Should().Be(2);
        memberships.Content.Value.Memberships![0].OrganizationId.Should().NotBeNull();
        memberships.Content.Value.Memberships![0].Ownership.Should().Be(OrganizationOwnership.Personal);
        memberships.Content.Value.Memberships![0].Roles.Should().ContainInOrder(TenantRoles.Owner.Name);
        memberships.Content.Value.Memberships![1].OrganizationId.Should().Be(organizationId);
        memberships.Content.Value.Memberships![1].Ownership.Should().Be(OrganizationOwnership.Shared);
        memberships.Content.Value.Memberships![1].Roles.Should().ContainInOrder(TenantRoles.Member.Name);
    }

    [Fact]
    public async Task WhenPromoteAndDemoteOwner_ThenDemotes()
    {
        var loginA = await LoginUserAsync();
        var loginB = await LoginUserAsync(LoginUser.PersonB);

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;
        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginB.User.Profile!.EmailAddress
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        await Api.PutAsync(new AssignRolesToOrganizationRequest
        {
            Id = organizationId,
            UserId = loginB.User.Id,
            Roles = [TenantRoles.Owner.Name]
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var memberships1 = await Api.GetAsync(new ListMembershipsForCallerRequest(),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        memberships1.Content.Value.Memberships!.Count.Should().Be(2);
        memberships1.Content.Value.Memberships![0].OrganizationId.Should().NotBeNull();
        memberships1.Content.Value.Memberships![0].Ownership.Should().Be(OrganizationOwnership.Personal);
        memberships1.Content.Value.Memberships![0].Roles.Should().ContainInOrder(TenantRoles.Owner.Name);
        memberships1.Content.Value.Memberships![1].OrganizationId.Should().Be(organizationId);
        memberships1.Content.Value.Memberships![1].Ownership.Should().Be(OrganizationOwnership.Shared);
        memberships1.Content.Value.Memberships![1].Roles.Should().ContainInOrder(TenantRoles.Owner.Name);

        await Api.PutAsync(new UnassignRolesFromOrganizationRequest
        {
            Id = organizationId,
            UserId = loginB.User.Id,
            Roles = [TenantRoles.Owner.Name]
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var memberships2 = await Api.GetAsync(new ListMembershipsForCallerRequest(),
            req => req.SetJWTBearerToken(loginB.AccessToken));

        memberships2.Content.Value.Memberships!.Count.Should().Be(2);
        memberships2.Content.Value.Memberships![0].OrganizationId.Should().NotBeNull();
        memberships2.Content.Value.Memberships![0].Ownership.Should().Be(OrganizationOwnership.Personal);
        memberships2.Content.Value.Memberships![0].Roles.Should().ContainInOrder(TenantRoles.Owner.Name);
        memberships2.Content.Value.Memberships![1].OrganizationId.Should().Be(organizationId);
        memberships2.Content.Value.Memberships![1].Ownership.Should().Be(OrganizationOwnership.Shared);
        memberships2.Content.Value.Memberships![1].Roles.Should().ContainInOrder(TenantRoles.Member.Name);
    }

    
    
    [Fact]
    public async Task WhenDeleteAndHasMembers_ThenReturnsError()
    {
        var loginA = await LoginUserAsync();
        var loginC = CreateRandomEmailAddress();

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;

        await Api.PostAsync(new InviteMemberToOrganizationRequest
        {
            Id = organizationId,
            Email = loginC
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        var result = await Api.DeleteAsync(new DeleteOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WhenDeleteAndHasNoMembers_ThenDeletes()
    {
        var loginA = await LoginUserAsync();

        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "aname"
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        loginA = await ReAuthenticateUserAsync(loginA.User);
        var organizationId = organization.Content.Value.Organization!.Id;

        var result = await Api.DeleteAsync(new DeleteOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(loginA.AccessToken));

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static string CreateRandomEmailAddress()
    {
        return $"aninvitee{++_invitationCount}@company.com";
    }
}