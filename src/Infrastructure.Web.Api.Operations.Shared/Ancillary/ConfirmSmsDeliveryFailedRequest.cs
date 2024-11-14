#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Confirms the failed delivery of a sent SMS message
/// </summary>
[Route("/smses/failed", OperationMethod.Post, isTestingOnly: true)]
public class ConfirmSmsDeliveryFailedRequest : UnTenantedEmptyRequest<ConfirmSmsDeliveryFailedRequest>
{
    public DateTime? FailedAtUtc { get; set; }

    public string? Reason { get; set; }

    [Required] public string? ReceiptId { get; set; }
}
#endif