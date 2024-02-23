using System.Text.Json;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Infrastructure.Web.Api.Common;

/// <summary>
///     Provides a detective that determines the tenant of the request from data within the request,
///     in either from the <see cref="ITenantedRequest.OrganizationId" /> field in the body,
///     from the query string or from the <see cref="HttpHeaders.Tenant" /> header.
/// </summary>
public class RequestTenantDetective : ITenantDetective
{
    public async Task<Result<TenantDetectionResult, Error>> DetectTenantAsync(HttpContext httpContext,
        Optional<Type> requestDtoType, CancellationToken cancellationToken)
    {
        var shouldHaveTenantId = IsTenantedRequest(requestDtoType);
        var (found, tenantIdFromRequest) = await ParseTenantIdFromRequestAsync(httpContext.Request, cancellationToken);
        if (found)
        {
            return new TenantDetectionResult(shouldHaveTenantId, tenantIdFromRequest);
        }

        return new TenantDetectionResult(shouldHaveTenantId, null);
    }

    private static bool IsTenantedRequest(Optional<Type> requestDtoType)
    {
        if (!requestDtoType.HasValue)
        {
            return false;
        }

        return requestDtoType.Value.IsAssignableTo(typeof(ITenantedRequest));
    }

    /// <summary>
    ///     Attempts to locate the tenant ID from the request query, or header, or body
    /// </summary>
    private static async Task<(bool HasTenantId, string? tenantId)> ParseTenantIdFromRequestAsync(HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Headers.TryGetValue(HttpHeaders.Tenant, out var tenantIdFromHeader))
        {
            var value = GetFirstStringValue(tenantIdFromHeader);
            if (value.HasValue())
            {
                return (true, value);
            }
        }

        if (request.Query.TryGetValue(HttpQueryParams.Tenant, out var tenantIdFromQueryString))
        {
            var value = GetFirstStringValue(tenantIdFromQueryString);
            if (value.HasValue())
            {
                return (true, value);
            }
        }

        var couldHaveBody = new HttpMethod(request.Method).CanHaveBody();
        if (couldHaveBody)
        {
            var (found, tenantIdFromRequestBody) = await ParseTenantIdFromRequestBodyAsync(request, cancellationToken);
            if (found)
            {
                return (true, tenantIdFromRequestBody);
            }
        }

        return (false, null);
    }

    private static async Task<(bool HasTenantId, string? tenantId)> ParseTenantIdFromRequestBodyAsync(
        HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.Body.Position != 0)
        {
            request.RewindBody();
        }

        if (request.ContentType == HttpContentTypes.Json)
        {
            try
            {
                var requestWithTenantId =
                    await request.ReadFromJsonAsync(typeof(RequestWithTenantId), cancellationToken);
                request.RewindBody();
                if (requestWithTenantId is RequestWithTenantId tenantId)
                {
                    if (tenantId.OrganizationId.HasValue())
                    {
                        return (true, tenantId.OrganizationId);
                    }

                    if (tenantId.TenantId.HasValue())
                    {
                        return (true, tenantId.TenantId);
                    }
                }
            }
            catch (JsonException)
            {
                return (false, null);
            }
        }

        if (request.ContentType == HttpContentTypes.FormUrlEncoded)
        {
            var form = await request.ReadFormAsync(cancellationToken);
            if (form.TryGetValue(nameof(ITenantedRequest.OrganizationId), out var tenantId))
            {
                var value = GetFirstStringValue(tenantId);
                if (value.HasValue())
                {
                    return (true, value);
                }
            }
        }

        return (false, null);
    }

    private static string? GetFirstStringValue(StringValues values)
    {
        return values.FirstOrDefault(value => value.HasValue());
    }

    /// <summary>
    ///     Defines a request that could have a tenant ID within it
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    internal class RequestWithTenantId : ITenantedRequest
    {
        public string? TenantId { get; [UsedImplicitly] set; }

        public string? OrganizationId { get; set; }
    }
}