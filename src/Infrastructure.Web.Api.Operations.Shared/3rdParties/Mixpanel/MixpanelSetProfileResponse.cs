using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;

public class MixpanelSetProfileResponse : IWebResponse
{
    [JsonPropertyName("error")] public string? Error { get; set; }

    [JsonPropertyName("status")] public int Status { get; set; }
}