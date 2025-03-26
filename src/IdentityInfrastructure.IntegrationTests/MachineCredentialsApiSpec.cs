using System.Net;
using ApiHost1;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.Organizations;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class MachineCredentialsApiSpec : WebApiSpec<Program>
{
    public MachineCredentialsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    [Fact]
    public async Task WhenRegisterMachineByAnonymous_ThenRegisters()
    {
        var result = await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        });

        result.Content.Value.Machine.Id.Should().NotBeEmpty();
        result.Content.Value.Machine.Description.Should().Be("amachinename");
        result.Content.Value.Machine.ApiKey.Should().StartWith("apk_");
        result.Content.Value.Machine.CreatedById.Should().Be(CallerConstants.AnonymousUserId);
        result.Content.Value.Machine.ExpiresOnUtc.Should().BeNull();
    }

    [Fact]
    public async Task WhenRegisterMachineByAuthenticatedUserInPersonalOrg_ThenRegistersMachineNotMember()
    {
        var login = await LoginUserAsync();

        var machine = await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        machine.Content.Value.Machine.Id.Should().NotBeEmpty();
        machine.Content.Value.Machine.Description.Should().Be("amachinename");
        machine.Content.Value.Machine.ApiKey.Should().StartWith("apk_");
        machine.Content.Value.Machine.CreatedById.Should().Be(login.User.Id);
        machine.Content.Value.Machine.ExpiresOnUtc.Should().BeNull();

        await PropagateDomainEventsAsync(PropagationRounds.Twice);
        var memberships = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = login.DefaultOrganizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        memberships.Content.Value.Members.Count.Should().Be(1);
        memberships.Content.Value.Members[0].UserId.Should().Be(login.User.Id);
        memberships.Content.Value.Members[0].IsOwner.Should().BeTrue();
    }

    [Fact]
    public async Task WhenRegisterMachineByAuthenticatedUserInSharedOrg_ThenRegistersMachineAsMember()
    {
        var login = await LoginUserAsync();
        var organization = await Api.PostAsync(new CreateOrganizationRequest
        {
            Name = "anorganization"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        login = await ReAuthenticateUserAsync(login);
        var organizationId = organization.Content.Value.Organization.Id;
        var machine = await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        machine.Content.Value.Machine.Id.Should().NotBeEmpty();
        machine.Content.Value.Machine.Description.Should().Be("amachinename");
        machine.Content.Value.Machine.ApiKey.Should().StartWith("apk_");
        machine.Content.Value.Machine.CreatedById.Should().Be(login.User.Id);
        machine.Content.Value.Machine.ExpiresOnUtc.Should().BeNull();

        var memberships = await Api.GetAsync(new ListMembersForOrganizationRequest
        {
            Id = organizationId
        }, req => req.SetJWTBearerToken(login.AccessToken));

        memberships.Content.Value.Members.Count.Should().Be(2);
        memberships.Content.Value.Members[0].UserId.Should().Be(login.User.Id);
        memberships.Content.Value.Members[0].IsOwner.Should().BeTrue();
        memberships.Content.Value.Members[1].UserId.Should().Be(machine.Content.Value.Machine.Id);
        memberships.Content.Value.Members[1].IsOwner.Should().BeFalse();
    }

    [Fact]
    public async Task WhenCallingSecureApiWithNewApiKey_ThenAuthenticates()
    {
        var machine = await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        });

        await PropagateDomainEventsAsync(PropagationRounds.Twice);
        var apiKey = machine.Content.Value.Machine.ApiKey;
#if TESTINGONLY
        var result = await Api.GetAsync(new GetCallerWithTokenOrAPIKeyTestingOnlyRequest(),
            req => req.SetAPIKey(apiKey));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.CallerId.Should().Be(machine.Content.Value.Machine.Id);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}