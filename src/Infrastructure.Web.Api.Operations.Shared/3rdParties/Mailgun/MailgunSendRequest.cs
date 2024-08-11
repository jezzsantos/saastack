using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;

/// <summary>
///     Sends an email
/// </summary>
[Route("/{DomainName}/messages", OperationMethod.Post)]
public class MailgunSendRequest : WebRequest<MailgunSendRequest, MailgunSendResponse>, IHasMultipartForm
{
    [JsonIgnore] public string? DomainName { get; set; }

    [JsonPropertyName("from")] public string? From { get; set; }

    [JsonPropertyName("html")] public string? Html { get; set; }

    [JsonPropertyName("recipient-variables")]
    public string? RecipientVariables { get; set; }

    [JsonPropertyName("subject")] public string? Subject { get; set; }

    [JsonPropertyName("o:testmode")] public string TestingOnly { get; set; } = "no";

    [JsonPropertyName("to")] public string? To { get; set; }

    [JsonPropertyName("o:tracking-opens")] public string Tracking { get; set; } = "no";
}