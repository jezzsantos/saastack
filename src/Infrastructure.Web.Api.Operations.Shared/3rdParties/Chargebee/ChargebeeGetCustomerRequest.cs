using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/customers?prod_cat_ver=2#retrieve_a_customer
/// </summary>
[Route("/customers/{Id}", OperationMethod.Get)]
public class ChargebeeGetCustomerRequest : UnTenantedRequest<ChargebeeGetCustomerRequest, ChargebeeGetCustomerResponse>
{
    public string? Id { get; set; }
}