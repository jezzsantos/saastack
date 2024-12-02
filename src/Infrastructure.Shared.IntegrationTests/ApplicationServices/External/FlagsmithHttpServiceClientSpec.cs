using Common;
using Common.Configuration;
using Common.Extensions;
using Common.FeatureFlags;
using Common.Recording;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.IntegrationTests.ApplicationServices.External;

[Trait("Category", "Integration.External")]
[Collection("EXTERNAL")]
public class FlagsmithHttpServiceClientSpec : ExternalApiSpec
{
    private const string TestTenant1 = "atenant1";
    private const string TestTenant2 = "atenant2";
    private const string TestUser1 = "auser1";
    private const string TestUser2 = "auser2";
    private static bool _isInitialized;
    private readonly FlagsmithHttpServiceClient _serviceClient;

    public FlagsmithHttpServiceClientSpec(ExternalApiSetup setup) : base(setup, OverrideDependencies)
    {
        var settings = setup.GetRequiredService<IConfigurationSettings>();
        _serviceClient = new FlagsmithHttpServiceClient(NoOpRecorder.Instance, settings, new TestHttpClientFactory());
        if (!_isInitialized)
        {
            _isInitialized = true;
            SetupTestingSandboxAsync().GetAwaiter().GetResult();
        }
    }

    [Fact]
    public async Task WhenGetAllFlags_ThenReturnsFlags()
    {
#if TESTINGONLY
        var result = await _serviceClient.GetAllFlagsAsync();

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value[0].Name.Should().Be(Flag.TestingOnly.Name);
        result.Value[0].IsEnabled.Should().BeFalse();
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncForAnUnknownFeature_ThenReturnsError()
    {
        var result = await _serviceClient.GetFlagAsync(new Flag("unknown"), Optional<string>.None,
            Optional<string>.None, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound,
            Resources.FlagsmithHttpServiceClient_UnknownFeature.Format("unknown"));
    }

    [Fact]
    public async Task WhenGetFlagAsyncForKnownFeatureWithNoOverrides_ThenReturnsDefaultFlag()
    {
#if TESTINGONLY
        var result = await _serviceClient.GetFlagAsync(Flag.TestingOnly, Optional<string>.None, Optional<string>.None,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Flag.TestingOnly.Name);
        result.Value.IsEnabled.Should().BeFalse();
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncForNoUserIdentity_ThenReturnsDefaultFlag()
    {
#if TESTINGONLY
        var result = await _serviceClient.GetFlagAsync(Flag.TestingOnly, "anunknowntenantid", Optional<string>.None,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Flag.TestingOnly.Name);
        result.Value.IsEnabled.Should().BeFalse();
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncForAnUnknownUserIdentity_ThenReturnsDefaultFlag()
    {
#if TESTINGONLY
        var result = await _serviceClient.GetFlagAsync(Flag.TestingOnly, Optional<string>.None, "anunknownuserid",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Flag.TestingOnly.Name);
        result.Value.IsEnabled.Should().BeFalse();
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncForAnonymousUserIdentity_ThenReturnsDefaultFlag()
    {
#if TESTINGONLY
        var result = await _serviceClient.GetFlagAsync(Flag.TestingOnly, Optional<string>.None,
            CallerConstants.AnonymousUserId,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Flag.TestingOnly.Name);
        result.Value.IsEnabled.Should().BeFalse();
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncForAOverriddenUserIdentity_ThenReturnsOverriddenFlag()
    {
#if TESTINGONLY
        var result = await _serviceClient.GetFlagAsync(Flag.TestingOnly, Optional<string>.None, TestUser1,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Flag.TestingOnly.Name);
        result.Value.IsEnabled.Should().BeTrue();
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncForAnUnknownMembershipIdentity_ThenReturnsDefaultFlag()
    {
#if TESTINGONLY
        var result =
            await _serviceClient.GetFlagAsync(Flag.TestingOnly, TestTenant2, TestUser2, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Flag.TestingOnly.Name);
        result.Value.IsEnabled.Should().BeFalse();
#endif
    }

    [Fact]
    public async Task WhenGetFlagAsyncForAOverriddenMembershipIdentity_ThenReturnsOverriddenFlag()
    {
#if TESTINGONLY
        var result =
            await _serviceClient.GetFlagAsync(Flag.TestingOnly, TestTenant1, TestUser1, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Name.Should().Be(Flag.TestingOnly.Name);
        result.Value.IsEnabled.Should().BeTrue();
#endif
    }

    [Fact]
    public void WhenIsEnabledForUnknownFeature_ThenReturnsFalse()
    {
        var result = _serviceClient.IsEnabled(new Flag("unknown"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsEnabledForUnknownFeatureWithNoOverrides_ThenReturnsFalse()
    {
#if TESTINGONLY
        var result = _serviceClient.IsEnabled(Flag.TestingOnly);

        result.Should().BeFalse();
#endif
    }

    [Fact]
    public void WhenIsEnabledForNoIdentity_ThenReturnsFalse()
    {
#if TESTINGONLY
        var result = _serviceClient.IsEnabled(Flag.TestingOnly, "anunknowntenantid", Optional<string>.None);

        result.Should().BeFalse();
#endif
    }

    [Fact]
    public void WhenIsEnabledForUnknownIdentity_ThenReturnsFalse()
    {
#if TESTINGONLY
        var result = _serviceClient.IsEnabled(Flag.TestingOnly, "anunknownuserid");

        result.Should().BeFalse();
#endif
    }

    [Fact]
    public void WhenIsEnabledForAnonymousUserIdentity_ThenReturnsFalse()
    {
#if TESTINGONLY
        var result = _serviceClient.IsEnabled(Flag.TestingOnly, CallerConstants.AnonymousUserId);

        result.Should().BeFalse();
#endif
    }

    [Fact]
    public void WhenIsEnabledForOverriddenIdentity_ThenReturnsTrue()
    {
#if TESTINGONLY
        var result = _serviceClient.IsEnabled(Flag.TestingOnly, TestUser1);

        result.Should().BeTrue();
#endif
    }

    [Fact]
    public void WhenIsEnabledForUnknownMembershipIdentity_ThenReturnsFalse()
    {
#if TESTINGONLY
        var result = _serviceClient.IsEnabled(Flag.TestingOnly, TestTenant2, TestUser2);

        result.Should().BeFalse();
#endif
    }

    [Fact]
    public void WhenIsEnabledForOverriddenMembershipIdentity_ThenReturnsTrue()
    {
#if TESTINGONLY
        var result = _serviceClient.IsEnabled(Flag.TestingOnly, TestTenant1, TestUser1);

        result.Should().BeTrue();
#endif
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //Do nothing
    }

    private async Task SetupTestingSandboxAsync()
    {
#if TESTINGONLY
        await _serviceClient.DestroyAllFeaturesAsync();
        await _serviceClient.DestroyAllIdentitiesAsync();
        await _serviceClient.CreateFeatureAsync(Flag.TestingOnly, false);
        await _serviceClient.CreateIdentityAsync(TestUser1, Flag.TestingOnly, true);
#endif
    }
}