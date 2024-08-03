using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/customers?prod_cat_ver=2#update_billing_info_for_a_customer
/// </summary>
[Route("/customers/{Id}/update_billing_info", OperationMethod.Post)]
[UsedImplicitly]
public class ChargebeeUpdateCustomerBillingInfoRequest : UnTenantedRequest<ChargebeeUpdateCustomerBillingInfoRequest,
    ChargebeeUpdateCustomerResponse>
{
    public ChargebeeAddress? BillingAddress { get; set; }

    public string? Id { get; set; }
}