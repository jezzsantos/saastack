using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Mixpanel;

public class MixpanelImportEventsResponse : IWebResponse
{
    [JsonPropertyName("code")] public required int Code { get; set; }

    [JsonPropertyName("num_records_imported")]
    public required int NumRecordsImported { get; set; }

    [JsonPropertyName("status")] public required string Status { get; set; }
}