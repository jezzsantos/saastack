using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared._3rdParties.Chargebee;

/// <summary>
///     Chargebee API: https://apidocs.chargebee.com/docs/api/customers?prod_cat_ver=2#create_a_customer
/// </summary>
[Route("/customers", OperationMethod.Post)]
public class
    ChargebeeCreateCustomerRequest : UnTenantedRequest<ChargebeeCreateCustomerRequest, ChargebeeCreateCustomerResponse>
{
    public ChargebeeAddress? BillingAddress { get; set; }

    public string? Company { get; set; }

    public string? Email { get; set; }

    public string? FirstName { get; set; }

    public string? Id { get; set; }

    public string? LastName { get; set; }

    public string? MetaData { get; set; }

    public string? Phone { get; set; }
}

public class ChargebeeAddress
{
}