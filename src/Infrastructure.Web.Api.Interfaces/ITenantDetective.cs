using Common;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Defines a detective that can be used to determine the tenant for a request
/// </summary>
public interface ITenantDetective
{
    /// <summary>
    ///     Returns the ID of the tenant for the specified request in the current <see cref="httpContext" />
    ///     that could be of type <see cref="requestDtoType" />,and also whether a tenant should exist for the specific request
    /// </summary>
    Task<Result<TenantDetectionResult, Error>> DetectTenantAsync(HttpContext httpContext,
        Optional<Type> requestDtoType, CancellationToken cancellationToken);
}

/// <summary>
///     Defines the result of a tenant detection
/// </summary>
public class TenantDetectionResult
{
    public TenantDetectionResult(bool shouldHaveTenantId, Optional<string> tenantId)
    {
        ShouldHaveTenantId = shouldHaveTenantId;
        TenantId = tenantId;
    }

    public bool ShouldHaveTenantId { get; }

    public Optional<string> TenantId { get; }
}