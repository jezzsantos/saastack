#if TESTINGONLY

using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;
using Infrastructure.Web.Interfaces.Clients;
using Flag = Common.FeatureFlags.Flag;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <inheritdoc cref="FlagsmithHttpServiceClient" />
partial class FlagsmithHttpServiceClient
{
    private const string TestingOnlyApiTokenSettingName = "ApplicationServices:Flagsmith:TestingOnly:ApiToken";
    private const string TestingOnlyEnvironmentApiKeySettingName =
        "ApplicationServices:Flagsmith:TestingOnly:EnvironmentApiKey";
    private const string TestingOnlyPrivateApiUrlSettingName = "ApplicationServices:Flagsmith:TestingOnly:BaseUrl";
    private const string TestingOnlyProjectIdSettingName = "ApplicationServices:Flagsmith:TestingOnly:ProjectId";
    private readonly TestingOnlyConfiguration _testingConfiguration;
    private readonly IServiceClient _testingOnlyClient;

    public async Task CreateFeatureAsync(Flag flag, bool enabled)
    {
        var featureCreated = await _testingOnlyClient.PostAsync(null, new FlagsmithCreateFeatureRequest
        {
            ProjectId = _testingConfiguration.ProjectId,
            Name = flag.Name
        }, req => AddApiToken(req, _testingConfiguration));
        var feature = featureCreated.Value;

        if (enabled)
        {
            var featureSetRetrieved = await _testingOnlyClient.GetAsync(null, new FlagsmithGetFeatureStatesRequest
            {
                EnvironmentApiKey = _testingConfiguration.EnvironmentApiKey,
                Feature = feature.Id
            }, req => AddApiToken(req, _testingConfiguration));

            var featureStateId = featureSetRetrieved.Value.Results[0].Id;
            await _testingOnlyClient.PatchAsync(null, new FlagsmithCreateFeatureStateRequest
            {
                EnvironmentApiKey = _testingConfiguration.EnvironmentApiKey,
                FeatureStateId = featureStateId,
                Feature = feature.Id,
                Enabled = enabled
            }, req => AddApiToken(req, _testingConfiguration));
        }
    }

    public async Task CreateIdentityAsync(string name, Flag flag, bool enabled)
    {
        var identityCreated = await _testingOnlyClient.PostAsync(null, new FlagsmithCreateEdgeIdentityRequest
        {
            EnvironmentApiKey = _testingConfiguration.EnvironmentApiKey,
            Identifier = name
        }, req => AddApiToken(req, _testingConfiguration));

        if (enabled)
        {
            var featuresRetrieved = await _testingOnlyClient.GetAsync(null, new FlagsmithGetFeaturesRequest
            {
                ProjectId = _testingConfiguration.ProjectId
            }, req => AddApiToken(req, _testingConfiguration));

            var featureId = featuresRetrieved.Value.Results.Single(feat => feat.Name == flag.Name).Id;
            await _testingOnlyClient.PostAsync(null,
                new FlagsmithCreateEdgeIdentityFeatureStateRequest
                {
                    EnvironmentApiKey = _testingConfiguration.EnvironmentApiKey,
                    IdentityUuid = identityCreated.Value.IdentityUuid!,
                    Feature = featureId,
                    Enabled = enabled
                }, req => AddApiToken(req, _testingConfiguration));
        }
    }

    public async Task DestroyAllFeaturesAsync()
    {
        var featuresRetrieved = await _testingOnlyClient.GetAsync(null, new FlagsmithGetFeaturesRequest
        {
            ProjectId = _testingConfiguration.ProjectId
        }, req => AddApiToken(req, _testingConfiguration));

        var allFeatures = featuresRetrieved.Value.Results;
        foreach (var feature in allFeatures)
        {
            await DestroyFeatureAsync(feature.Id);
        }
    }

    public async Task DestroyAllIdentitiesAsync()
    {
        var identitiesRetrieved = await _testingOnlyClient.GetAsync(null, new FlagsmithGetEdgeIdentitiesRequest
        {
            EnvironmentApiKey = _testingConfiguration.EnvironmentApiKey
        }, req => AddApiToken(req, _testingConfiguration));

        var allIdentities = identitiesRetrieved.Value.Results;
        foreach (var identity in allIdentities)
        {
            await DestroyIdentityAsync(identity.IdentityUuid!);
        }
    }

    private async Task DestroyFeatureAsync(int featureId)
    {
        await _testingOnlyClient.DeleteAsync(null, new FlagsmithDeleteFeatureRequest
        {
            ProjectId = _testingConfiguration.ProjectId,
            FeatureId = featureId
        }, req => AddApiToken(req, _testingConfiguration));
    }

    private async Task DestroyIdentityAsync(string identityUuid)
    {
        await _testingOnlyClient.DeleteAsync(null, new FlagsmithDeleteEdgeIdentitiesRequest
        {
            EnvironmentApiKey = _testingConfiguration.EnvironmentApiKey,
            IdentityUuid = identityUuid
        }, req => AddApiToken(req, _testingConfiguration));
    }

    private static void AddApiToken(HttpRequestMessage req, TestingOnlyConfiguration configuration)
    {
        req.Headers.Add(HttpConstants.Headers.Authorization, $"Token {configuration.ApiToken}");
    }

    public class TestingOnlyConfiguration
    {
        public required string ApiToken { get; init; }

        public required string ApiUrl { get; init; }

        public required string EnvironmentApiKey { get; init; }

        public required int ProjectId { get; init; }
    }
}
#endif