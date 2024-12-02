using System.Text.Json.Serialization;
using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Twilio;

public class TwilioSendResponse : IWebResponse
{
    [JsonPropertyName("body")] public required string Body { get; set; }

    [JsonPropertyName("sid")] public string? Sid { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("status")] public TwilioMessageStatus? Status { get; set; }
}