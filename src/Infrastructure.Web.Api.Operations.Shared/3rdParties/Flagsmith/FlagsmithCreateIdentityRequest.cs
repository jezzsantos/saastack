using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Flagsmith;

[Route("/identities/", OperationMethod.Post)]
[UsedImplicitly]
public class FlagsmithCreateIdentityRequest : IWebRequest<FlagsmithCreateIdentityResponse>
{
    [JsonPropertyName("identifier")] public required string Identifier { get; set; }

    [JsonPropertyName("traits")] public required List<FlagsmithTrait> Traits { get; set; }
}