#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Confirms the failed delivery of a sent email message
/// </summary>
[Route("/emails/failed", OperationMethod.Post, isTestingOnly: true)]
public class ConfirmEmailDeliveryFailedRequest : UnTenantedEmptyRequest<ConfirmEmailDeliveryFailedRequest>
{
    public DateTime? FailedAtUtc { get; set; }

    public string? Reason { get; set; }

    [Required] public string? ReceiptId { get; set; }
}
#endif