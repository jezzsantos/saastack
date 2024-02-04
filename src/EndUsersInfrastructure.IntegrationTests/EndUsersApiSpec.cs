using ApiHost1;
using Domain.Interfaces.Authorization;
using FluentAssertions;
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
    public EndUsersApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
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

    private static void OverrideDependencies(IServiceCollection services)
    {
    }
}