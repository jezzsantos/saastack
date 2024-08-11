#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Confirms the delivery of a sent email message
/// </summary>
[Route("/emails/delivered", OperationMethod.Post, isTestingOnly: true)]
public class ConfirmEmailDeliveredRequest : UnTenantedEmptyRequest<ConfirmEmailDeliveredRequest>
{
    public DateTime? DeliveredAtUtc { get; set; }

    [Required] public string? ReceiptId { get; set; }
}
#endif