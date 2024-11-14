#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Ancillary;

/// <summary>
///     Confirms the delivery of a sent SMS message
/// </summary>
[Route("/smses/delivered", OperationMethod.Post, isTestingOnly: true)]
public class ConfirmSmsDeliveredRequest : UnTenantedEmptyRequest<ConfirmSmsDeliveredRequest>
{
    public DateTime? DeliveredAtUtc { get; set; }

    [Required] public string? ReceiptId { get; set; }
}
#endif