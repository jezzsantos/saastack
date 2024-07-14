using Application.Interfaces;
using Application.Resources.Shared;
using Common.Configuration;
using Common.Recording;
using Domain.Interfaces;
using Infrastructure.Shared.ApplicationServices.External;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.IntegrationTests.ApplicationServices.External;

[Trait("Category", "Integration.External")]
[Collection("External")]
public class UserPilotHttpServiceClientSpec : ExternalApiSpec
{
    private readonly UserPilotHttpServiceClient _serviceClient;

    public UserPilotHttpServiceClientSpec(ExternalApiSetup setup) : base(setup, OverrideDependencies)
    {
        var settings = setup.GetRequiredService<IConfigurationSettings>();
        _serviceClient = new UserPilotHttpServiceClient(NoOpRecorder.Instance, settings, new TestHttpClientFactory());
    }

    [Fact]
    public async Task WhenDeliverAsyncWithNonIdentifiableEventByAnonymousUntenanted_ThenTracksEvent()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller(), CallerConstants.AnonymousUserId, "aneventname",
            new Dictionary<string, string>
            {
                { "aname", "avalue" }
            });

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDeliverAsyncWithNonIdentifiableEventByUserUntenanted_ThenTracksEvent()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller(), "user_1234567890123456789012", "aneventname",
            new Dictionary<string, string>
            {
                { "aname", "avalue" }
            });

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDeliverAsyncWithNonIdentifiableEventByAnonymousTenanted_ThenTracksEvent()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller("org_1234567890123456789012"),
            CallerConstants.AnonymousUserId, "aneventname",
            new Dictionary<string, string>
            {
                { "aname", "avalue" }
            });

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDeliverAsyncWithNonIdentifiableEventByUserTenanted_ThenTracksEvent()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller("org_1234567890123456789012"),
            "user_1234567890123456789012", "aneventname",
            new Dictionary<string, string>
            {
                { "aname", "avalue" }
            });

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDeliverAsyncWithUserLoginEvent_ThenIdentifiesAndTracks()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller(), "auserid",
            UsageConstants.Events.UsageScenarios.Generic.UserLogin, new Dictionary<string, string>
            {
                { UsageConstants.Properties.UserIdOverride, "user_1234567890123456789012" },
                { UsageConstants.Properties.AuthProvider, "credentials" },
                { UsageConstants.Properties.Name, "aperson" },
                { UsageConstants.Properties.EmailAddress, "aperson@company.com" },
                { UsageConstants.Properties.DefaultOrganizationId, "org_1234567890123456789012" }
            });

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDeliverAsyncWithOrganizationCreatedEvent_ThenIdentifiesAndTracks()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller(), "auserid",
            UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated, new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "org_1234567890123456789012" },
                { UsageConstants.Properties.Name, "anorganization" },
                { UsageConstants.Properties.Ownership, OrganizationOwnership.Shared.ToString() },
                { UsageConstants.Properties.CreatedById, "user_1234567890123456789012" },
                { UsageConstants.Properties.UserIdOverride, "user_1234567890123456789012" }
            });

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDeliverAsyncWithMembershipAddedEvent_ThenIdentifiesAndTracks()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller(), "auserid",
            UsageConstants.Events.UsageScenarios.Generic.MembershipAdded, new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "membership_1234567890123456789012" },
                { UsageConstants.Properties.TenantIdOverride, "org_1234567890123456789012" },
                { UsageConstants.Properties.UserIdOverride, "user_1234567890123456789012" }
            });

        result.Should().BeSuccess();
    }

    [Fact]
    public async Task WhenDeliverAsyncWithMembershipChangedEvent_ThenIdentifiesAndTracks()
    {
        var result = await _serviceClient.DeliverAsync(new TestCaller(), "auserid",
            UsageConstants.Events.UsageScenarios.Generic.MembershipChanged, new Dictionary<string, string>
            {
                { UsageConstants.Properties.Id, "membership_1234567890123456789012" },
                { UsageConstants.Properties.TenantIdOverride, "org_1234567890123456789012" },
                { UsageConstants.Properties.UserIdOverride, "user_1234567890123456789012" },
                { UsageConstants.Properties.Name, "aperson" },
                { UsageConstants.Properties.EmailAddress, "aperson@company.com" }
            });

        result.Should().BeSuccess();
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //Do nothing
    }
}