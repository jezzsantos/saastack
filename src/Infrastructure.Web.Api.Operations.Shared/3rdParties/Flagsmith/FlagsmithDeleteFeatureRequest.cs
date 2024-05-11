using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/projects/{ProjectId}/features/{FeatureId}/", OperationMethod.Delete)]
public class FlagsmithDeleteFeatureRequest : IWebRequest<EmptyResponse>
{
    public int? FeatureId { get; set; }

    public int? ProjectId { get; set; }
}