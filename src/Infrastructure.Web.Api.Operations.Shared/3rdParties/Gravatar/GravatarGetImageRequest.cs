using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Gravatar;

[Route("/avatar/{Hash}", OperationMethod.Get)]
[UsedImplicitly]
public class GravatarGetImageRequest : IWebRequestStream
{
    [JsonPropertyName("d")] public string? Default { get; set; }

    public required string Hash { get; set; }

    [JsonPropertyName("s")] public int? Width { get; set; }
}