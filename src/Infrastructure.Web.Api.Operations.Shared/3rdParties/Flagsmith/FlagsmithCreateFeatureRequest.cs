using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Creates a new feature
/// </summary>
[Route("/projects/{ProjectId}/features/", OperationMethod.Post)]
public class FlagsmithCreateFeatureRequest : WebRequest<FlagsmithCreateFeatureRequest, FlagsmithCreateFeatureResponse>
{
    [JsonPropertyName("name")] public string? Name { get; set; }

    public int ProjectId { get; set; }
}