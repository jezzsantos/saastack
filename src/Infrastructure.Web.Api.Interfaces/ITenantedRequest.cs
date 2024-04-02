namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request for a specific Tenant
/// </summary>
public interface ITenantedRequest
{
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines a request for a specific tenant, for Organization requests that are untenanted
///     Only to be used by the Organizations subdomain
/// </summary>
public interface IUnTenantedOrganizationRequest
{
    public string? Id { get; set; }
}