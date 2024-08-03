using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

public class ChargebeeListFeaturesResponse : IWebResponse
{
    public List<ChargebeeFeatureList>? List { get; set; }
}

[UsedImplicitly]
public class ChargebeeFeatureList
{
    public ChargebeeFeature? Feature { get; set; }
}

[UsedImplicitly]
public class ChargebeeFeature
{
}