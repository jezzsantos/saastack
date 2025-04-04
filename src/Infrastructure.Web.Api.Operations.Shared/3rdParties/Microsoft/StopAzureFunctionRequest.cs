using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Microsoft;

/// <summary>
///     Disables an Azure Function
/// </summary>
[Interfaces.Route(
    "/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Web/sites/{FunctionAppName}/functions/{FunctionName}/state",
    OperationMethod.PutPatch)]
[UsedImplicitly]
public class StopAzureFunctionRequest : UnTenantedRequest<StopAzureFunctionRequest, StopAzureFunctionResponse>
{
    [FromQuery]
    [JsonIgnore]
    [JsonPropertyName("api-version")]
    public string? ApiVersion { get; set; } = "2022-09-01";

    [JsonIgnore] [Required] public string? FunctionAppName { get; set; }

    [JsonIgnore] [Required] public string? FunctionName { get; set; }

    [Required] public string Properties { get; set; } = "disabled";

    [JsonIgnore] [Required] public string? ResourceGroupName { get; set; }

    [JsonIgnore] [Required] public string? SubscriptionId { get; set; }
}