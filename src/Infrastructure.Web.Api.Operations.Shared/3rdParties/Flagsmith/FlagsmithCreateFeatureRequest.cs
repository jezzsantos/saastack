using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/projects/{ProjectId}/features/", OperationMethod.Post)]
public class FlagsmithCreateFeatureRequest : IWebRequest<FlagsmithCreateFeatureResponse>
{
    [JsonPropertyName("name")] public string? Name { get; set; }

    public int ProjectId { get; set; }
}