using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Microsoft;

/// <summary>
///     Stops an Azure Function
/// </summary>
[Interfaces.Route(
    "/subscriptions/{SubscriptionId}/resourceGroups/{ResourceGroupName}/providers/Microsoft.Web/sites/{FunctionName}/stop",
    OperationMethod.Post)]
[UsedImplicitly]
public class StopAzureFunctionRequest : TenantedRequest<StopAzureFunctionRequest, StopAzureFunctionResponse>
{
    [FromQuery]
    [JsonPropertyName("api-version")]
    public string? ApiVersion { get; set; } = "2024-04-01";

    [Required] public string? FunctionName { get; set; }

    [Required] public string? ResourceGroupName { get; set; }

    [Required] public string? SubscriptionId { get; set; }
}