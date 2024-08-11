using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Gravatar;

/// <summary>
///     Fetches a gravatar image for the specified hash
/// </summary>
[Route("/avatar/{Hash}", OperationMethod.Get)]
[UsedImplicitly]
public class GravatarGetImageRequest : WebRequestStream<GravatarGetImageRequest>
{
    [JsonPropertyName("d")] public string? Default { get; set; }

    public string? Hash { get; set; }

    [JsonPropertyName("s")] public int? Width { get; set; }
}