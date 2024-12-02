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

[Trait("Category", "Integration.API")]
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
            Roles = [PlatformRoles.TestingOnly.Name]
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.User.Roles.Should()
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
            Roles = [PlatformRoles.TestingOnly.Name]
        }, req => req.SetJWTBearerToken(login.AccessToken));

        var result = await Api.PatchAsync(new UnassignPlatformRolesRequest
        {
            Id = login.User.Id,
            Roles = [PlatformRoles.TestingOnly.Name]
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.User.Roles.Should()
            .ContainInOrder(PlatformRoles.Operations.Name, PlatformRoles.Standard.Name);
#endif
    }

    [Fact]
    public async Task WhenAssignAndUnassignOperatorRoles_ThenRemainsStandardRole()
    {
        var @operator = await LoginUserAsync(LoginUser.Operator);
        var loginA = await LoginUserAsync();
#if TESTINGONLY
        var result1 = await Api.PostAsync(new AssignPlatformRolesRequest
        {
            Id = loginA.User.Id,
            Roles = [PlatformRoles.Operations.Name]
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

        result1.Content.Value.User.Roles.Should()
            .ContainInOrder(PlatformRoles.Operations.Name, PlatformRoles.Standard.Name);

        var result2 = await Api.PatchAsync(new UnassignPlatformRolesRequest
        {
            Id = loginA.User.Id,
            Roles = [PlatformRoles.Operations.Name]
        }, req => req.SetJWTBearerToken(@operator.AccessToken));

        result2.Content.Value.User.Roles.Should()
            .OnlyContain(rol => rol == PlatformRoles.Standard.Name);
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        services.AddSingleton<IEventNotificationMessageBroker, StubEventNotificationMessageBroker>();
    }
}