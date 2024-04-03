using ApiHost1;
using Domain.Interfaces.Authorization;
using EndUsersInfrastructure.IntegrationTests.Stubs;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.EndUsers;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EndUsersInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class EndUsersApiSpec : WebApiSpec<Program>
{
    private readonly StubEventNotificationMessageBroker _messageBroker;

    public EndUsersApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
        _messageBroker = setup.GetRequiredService<IEventNotificationMessageBroker>()
            .As<StubEventNotificationMessageBroker>();
        _messageBroker.Reset();
    }

    [Fact]
    public async Task WhenRegisterUser_ThenPublishesRegistrationIntegrationEvent()
    {
        var login = await LoginUserAsync();

        _messageBroker.LastPublishedEvent!.RootId.Should().Be(login.User.Id);
    }

    [Fact]
    public async Task WhenAssignPlatformRoles_ThenAssignsRoles()
    {
        var login = await LoginUserAsync(LoginUser.Operator);
#if TESTINGONLY
        var result = await Api.PostAsync(new AssignPlatformRolesRequest
        {
            Id = login.User.Id,
            Roles = new List<string> { PlatformRoles.TestingOnly.Name }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.User!.Roles.Should()
            .ContainInOrder(PlatformRoles.Standard.Name, PlatformRoles.TestingOnly.Name);
#endif
    }

    [Fact]
    public async Task WhenUnassignPlatformRoles_ThenAssignsRoles()
    {
        var login = await LoginUserAsync(LoginUser.Operator);
#if TESTINGONLY
        await Api.PostAsync(new AssignPlatformRolesRequest
        {
            Id = login.User.Id,
            Roles = new List<string> { PlatformRoles.TestingOnly.Name }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.PatchAsync(new UnassignPlatformRolesRequest
        {
            Id = login.User.Id,
            Roles = new List<string> { PlatformRoles.TestingOnly.Name }
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.User!.Roles.Should()
            .ContainInOrder(PlatformRoles.Standard.Name, PlatformRoles.Operations.Name);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IEventNotificationMessageBroker, StubEventNotificationMessageBroker>();
    }
}