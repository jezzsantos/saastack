using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/projects/{ProjectId}/features/", ServiceOperation.Post)]
public class FlagsmithCreateFeatureRequest : IWebRequest<FlagsmithCreateFeatureResponse>
{
    [JsonPropertyName("name")] public required string Name { get; set; }

    public required int ProjectId { get; set; }
}