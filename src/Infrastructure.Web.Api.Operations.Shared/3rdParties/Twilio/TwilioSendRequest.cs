using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Twilio;

/// <summary>
///     Sends an SMS
/// </summary>
[Route("/2010-04-01/Accounts/{AccountSid}/Messages.json", OperationMethod.Post)]
public class TwilioSendRequest : WebRequest<TwilioSendRequest, TwilioSendResponse>, IHasFormUrlEncoded
{
    [JsonIgnore] public string? AccountSid { get; set; }

    public string? Body { get; set; }

    public string? From { get; set; }

    public string? StatusCallback { get; set; }

    public string? To { get; set; }
}