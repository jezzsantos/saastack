using System.Net;
using ApiHost1;
using Common.Extensions;
using Domain.Interfaces;
using FluentAssertions;
using IdentityApplication;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.Identities;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common.Validation;
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
        result.Content.Value.Machine.ExpiresOnUtc!.Value.Should().BeNear(
            DateTime.UtcNow.ToNearestMinute().Add(APIKeysApplication.DefaultAPIKeyExpiry), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task WhenRegisterMachineByUser_ThenRegisters()
    {
        var login = await LoginUserAsync();

        var result = await Api.PostAsync(new RegisterMachineRequest
        {
            Name = "amachinename"
        }, req => req.SetJWTBearerToken(login.AccessToken));

        result.Content.Value.Machine.Id.Should().NotBeEmpty();
        result.Content.Value.Machine.Description.Should().Be("amachinename");
        result.Content.Value.Machine.ApiKey.Should().StartWith("apk_");
        result.Content.Value.Machine.CreatedById.Should().Be(login.User.Id);
        result.Content.Value.Machine.ExpiresOnUtc!.Value.Should().BeNear(
            DateTime.UtcNow.ToNearestMinute().Add(APIKeysApplication.DefaultAPIKeyExpiry), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task WhenCallingSecureApiWithMachineApiKey_ThenReturnsResponse()
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