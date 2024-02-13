using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/projects/{ProjectId}/features/{FeatureId}/", ServiceOperation.Delete)]
public class FlagsmithDeleteFeatureRequest : IWebRequest<EmptyResponse>
{
    public required int FeatureId { get; set; }

    public required int ProjectId { get; set; }
}