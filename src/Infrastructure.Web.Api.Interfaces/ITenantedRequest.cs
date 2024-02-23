namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines the request for a specific Tenant
/// </summary>
public interface ITenantedRequest
{
    public string? OrganizationId { get; set; }
}