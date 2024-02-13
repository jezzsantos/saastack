using Common.FeatureFlags;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.BackEndForFrontEnd;

public class GetAllFeatureFlagsResponse : IWebResponse
{
    public List<FeatureFlag> Flags { get; set; } = new();
}