using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Fetches all features for a project
/// </summary>
[Route("/projects/{ProjectId}/features/", OperationMethod.Get)]
public class FlagsmithGetFeaturesRequest : IWebRequest<FlagsmithGetFeaturesResponse>
{
    public int? ProjectId { get; set; }
}