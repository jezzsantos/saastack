using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;

public class MailgunSendMessageResponse : IWebResponse
{
    [JsonPropertyName("id")] public required string Id { get; set; }

    [JsonPropertyName("message")] public required string Message { get; set; }
}