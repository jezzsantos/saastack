using System.Text.Json;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.FeatureFlags;
using Domain.Interfaces;
using Flagsmith;
using Infrastructure.Web.Common.Clients;
using Flag = Common.FeatureFlags.Flag;

namespace Infrastructure.Shared.ApplicationServices.External;

/// <summary>
///     Provides an adapter to the feature flagging services of FlagSmith.com
///     <see href="https://docs.flagsmith.com/clients/server-side?language=dotnet" />
///     Note: Flagsmith already supports caching and optimizations like LocalEvaluation to limit the number of network
///     calls made, so we don't need to implement explicit caching.
///     Note: For AWS when running this process in a Serverless environment like AWS Lambda,
///     Flagsmith's local evaluation mode is not likely to work very well since it expects to be connected to the API at
///     all times, and Lambdas will shutdown automatically. See
///     <see href="https://docs.flagsmith.com/clients/overview">Overview</see>
///     * When calling <see cref="GetFlagAsync" /> for a flag that does not exist, we will return
///     <see cref="Error.EntityNotFound" />
///     * When calling <see cref="IsEnabled(Flag)" /> for a flag that does not exist, we will return
///     <see cref="FlagEnabledWhenNotExists" />
///     * We never want to ask for the flag for the anonymous user (<see cref="CallerConstants.AnonymousUserId" />)
///     Flagsmith configuration:
///     1. In flagsmith, we assume that there might be an identity for each user of interest, where its name is the ID of
///     the user, and it has a trait called 'type' with a value of "user"
///     2. In flagsmith, we assume that for every membership to a tenant, the user will have a trait that will be called
///     the ID of the tenant and have a value of "tenant"
///     3. When we ask for a flag for a userId, we create the identity with its respective traits
///     (if they don't already exist), and use the result.
/// </summary>
public partial class FlagsmithHttpServiceClient : IFeatureFlags
{
    private const string BaseUrlSettingName = "ApplicationServices:Flagsmith:BaseUrl";
    private const string EnvironmentKeySettingName = "ApplicationServices:Flagsmith:EnvironmentKey";
    private const bool FlagEnabledWhenNotExists = false;
    private const string FlagsmithApiUrl = "https://edge.api.flagsmith.com/api/v1/";
    private const string TraitNameForIdentityType = "type";
    private const string TraitValueForTenancyMembership = "tenant";
    private const string TraitValueForUser = "user";
    private const string UnknownFeatureName = "_unknown";
    private readonly FlagsmithClient _client;
    private readonly IRecorder _recorder;

    public FlagsmithHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory)
    {
        _recorder = recorder;
        var apiUrl = settings.Platform.GetString(BaseUrlSettingName, FlagsmithApiUrl);
        var environmentKey = settings.Platform.GetString(EnvironmentKeySettingName);
#if TESTINGONLY
        _testingConfiguration = new TestingOnlyConfiguration
        {
            ApiUrl = settings.Platform.GetString(TestingOnlyPrivateApiUrlSettingName, string.Empty),
            ApiToken = settings.Platform.GetString(TestingOnlyApiTokenSettingName, string.Empty),
            ProjectId = (int)settings.Platform.GetNumber(TestingOnlyProjectIdSettingName, 0),
            EnvironmentApiKey = settings.Platform.GetString(TestingOnlyEnvironmentApiKeySettingName, string.Empty)
        };
#endif
        var httpClient = httpClientFactory.CreateClient("Flagsmith");
        _client = new FlagsmithClient(new FlagsmithConfiguration
        {
            EnvironmentKey = environmentKey,
            ApiUrl = apiUrl,
            Retries = 1,
            CacheConfig = new CacheConfig(true)
            {
                DurationInMinutes = 5
            },
#if TESTINGONLY || HOSTEDONAWS
            EnableClientSideEvaluation = false,
#elif HOSTEDONAZURE
            EnableClientSideEvaluation = true,
#endif
            DefaultFlagHandler = _ => new Flagsmith.Flag(new Feature(UnknownFeatureName, -1), false, null, -1)
        }, httpClient);
#if TESTINGONLY
        _testingOnlyClient =
            new ApiServiceClient(httpClientFactory, JsonSerializerOptions.Default, _testingConfiguration.ApiUrl);
#endif
    }

    public async Task<Result<IReadOnlyList<FeatureFlag>, Error>> GetAllFlagsAsync(
        CancellationToken cancellationToken = default)
    {
        var environmentFlags = await _client.GetEnvironmentFlags();
        var allFlags = environmentFlags!.AllFlags().Select(flag => new FeatureFlag
            {
                Name = flag.GetFeatureName(),
                IsEnabled = flag.Enabled
            })
            .ToList();

        _recorder.TraceInformation(null, "Fetched all feature flags from FlagSmith API");
        return allFlags;
    }

    public async Task<Result<FeatureFlag, Error>> GetFlagAsync(Flag flag, Optional<string> tenantId,
        Optional<string> userId, CancellationToken cancellationToken)
    {
        IFlags? featureFlags;
        if (userId.HasValue && userId != CallerConstants.AnonymousUserId)
        {
            if (tenantId.HasValue)
            {
                featureFlags = await QueryForUserMembershipAsync(tenantId, userId, cancellationToken);
            }
            else
            {
                featureFlags = await QueryForUserAsync(userId, cancellationToken);
            }
        }
        else
        {
            featureFlags = await _client.GetEnvironmentFlags();
        }

        var featureFlag = await featureFlags.GetFlag(flag.Name);
        if (IsDefaultFeatureFlag(featureFlag))
        {
            return Error.EntityNotFound(Resources.FlagsmithHttpServiceClient_UnknownFeature.Format(flag.Name));
        }

        _recorder.TraceInformation(null, "Fetched feature flag for {Name}, for {User} from FlagSmith API",
            flag.Name, userId.HasValue
                ? userId
                : "allusers");

        return new FeatureFlag
        {
            Name = featureFlag.GetFeatureName(),
            IsEnabled = featureFlag.Enabled
        };
    }

    public bool IsEnabled(Flag flag)
    {
        var featureFlag = GetFlagAsync(flag, Optional<string>.None, Optional<string>.None, CancellationToken.None)
            .GetAwaiter().GetResult();
        if (!featureFlag.IsSuccessful)
        {
            return FlagEnabledWhenNotExists;
        }

        return featureFlag.Value.IsEnabled;
    }

    public bool IsEnabled(Flag flag, string userId)
    {
        var featureFlag = GetFlagAsync(flag, Optional<string>.None, userId, CancellationToken.None).GetAwaiter()
            .GetResult();
        if (!featureFlag.IsSuccessful)
        {
            return FlagEnabledWhenNotExists;
        }

        return featureFlag.Value.IsEnabled;
    }

    public bool IsEnabled(Flag flag, Optional<string> tenantId, string userId)
    {
        var featureFlag = GetFlagAsync(flag, tenantId, userId, CancellationToken.None).GetAwaiter().GetResult();
        if (!featureFlag.IsSuccessful)
        {
            return FlagEnabledWhenNotExists;
        }

        return featureFlag.Value.IsEnabled;
    }

    private static bool IsDefaultFeatureFlag(IFlag featureFlag)
    {
        return featureFlag.NotExists()
               || featureFlag.getFeatureId() == -1
               || featureFlag.GetFeatureName() == UnknownFeatureName;
    }

    private async Task<IFlags> QueryForUserAsync(string userId, CancellationToken cancellationToken)
    {
        var traits = new List<ITrait>
        {
            new Trait(TraitNameForIdentityType, TraitValueForUser)
        };

        return await QueryIdentityFlags(userId, traits, cancellationToken);
    }

    private async Task<IFlags> QueryForUserMembershipAsync(string tenantId, string userId,
        CancellationToken cancellationToken)
    {
        var userTraits = new List<ITrait>
        {
            new Trait(TraitNameForIdentityType, TraitValueForUser),
            new Trait(tenantId, TraitValueForTenancyMembership)
        };

        return await QueryIdentityFlags(userId, userTraits, cancellationToken);
    }

    private async Task<IFlags> QueryIdentityFlags(string identity, List<ITrait> traits,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Note: Will create this identity in Flagsmith if it does not yet exist!
        // Note: will add the traits to the identity if they do not exist!
        return await _client.GetIdentityFlags(identity, traits);
    }
}