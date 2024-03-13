using Common.FeatureFlags;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;
using WebsiteHost.Application;

namespace WebsiteHost.Api.FeatureFlags;

public class FeatureFlagsApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly IFeatureFlagsApplication _featureFlagsApplication;

    public FeatureFlagsApi(ICallerContextFactory contextFactory, IFeatureFlagsApplication featureFlagsApplication)
    {
        _contextFactory = contextFactory;
        _featureFlagsApplication = featureFlagsApplication;
    }

    public async Task<ApiGetResult<List<FeatureFlag>, GetAllFeatureFlagsResponse>> GetAll(
        GetAllFeatureFlagsRequest request,
        CancellationToken cancellationToken)
    {
        var flags = await _featureFlagsApplication.GetAllFeatureFlagsAsync(_contextFactory.Create(), cancellationToken);

        return () => flags.HandleApplicationResult(f => new GetAllFeatureFlagsResponse { Flags = f });
    }

    public async Task<ApiGetResult<FeatureFlag, GetFeatureFlagResponse>> GetForCaller(
        GetFeatureFlagForCallerRequest request,
        CancellationToken cancellationToken)
    {
        var flag = await _featureFlagsApplication.GetFeatureFlagForCallerAsync(_contextFactory.Create(),
            request.Name, cancellationToken);

        return () => flag.HandleApplicationResult(f => new GetFeatureFlagResponse { Flag = f });
    }
}