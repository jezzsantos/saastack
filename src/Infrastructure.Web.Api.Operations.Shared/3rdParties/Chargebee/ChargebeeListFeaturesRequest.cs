using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/features?prod_cat_ver=2#list_features
/// </summary>
[Route("/features", OperationMethod.Get)]
[UsedImplicitly]
public class
    ChargebeeListFeaturesRequest : UnTenantedRequest<ChargebeeListFeaturesRequest, ChargebeeListFeaturesResponse>
{
    public int? Limit { get; set; }
}