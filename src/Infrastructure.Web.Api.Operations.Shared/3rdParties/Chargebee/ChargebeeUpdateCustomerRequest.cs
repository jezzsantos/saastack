using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/customers?prod_cat_ver=2#update_a_customer
/// </summary>
[Route("/customers/{Id}", OperationMethod.Post)]
[UsedImplicitly]
public class
    ChargebeeUpdateCustomerRequest : UnTenantedRequest<ChargebeeUpdateCustomerRequest, ChargebeeUpdateCustomerResponse>
{
    public string? Company { get; set; }

    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? Id { get; set; }

    public string? LastName { get; set; }

    public string? MetaData { get; set; }

    public string? Phone { get; set; }
}