using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

/// <summary>
///     Creates a new identity
/// </summary>
[Route("/identities/", OperationMethod.Post)]
[UsedImplicitly]
public class
    FlagsmithCreateIdentityRequest : WebRequest<FlagsmithCreateIdentityRequest, FlagsmithCreateIdentityResponse>
{
    [JsonPropertyName("identifier")] public string? Identifier { get; set; }

    [JsonPropertyName("traits")] public List<FlagsmithTrait> Traits { get; set; } = new();
}