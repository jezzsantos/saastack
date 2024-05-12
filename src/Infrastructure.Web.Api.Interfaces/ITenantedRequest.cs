using System.ComponentModel;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a request for a specific tenant
/// </summary>
public interface ITenantedRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? OrganizationId { get; set; }
}

/// <summary>
///     Defines a request for a specific tenant, for Organization requests that are untenanted
///     Only to be used by the Organizations subdomain
/// </summary>
public interface IUnTenantedOrganizationRequest
{
    [Description(
        "An ID of the Organization. If not provided, the ID of the default organization of the authenticated user (if any) is used")]
    public string? Id { get; set; }
}