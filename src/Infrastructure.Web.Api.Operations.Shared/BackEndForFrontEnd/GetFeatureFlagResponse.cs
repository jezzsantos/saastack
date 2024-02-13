using Common.FeatureFlags;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

public class GetFeatureFlagResponse : IWebResponse
{
    public FeatureFlag? Flag { get; set; }
}