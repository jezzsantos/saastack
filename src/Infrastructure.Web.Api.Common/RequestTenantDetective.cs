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
///     from one of these sources:
///     1. The <see cref="ITenantedRequest.OrganizationId" /> field in the body,
///     2. The <see cref="IUnTenantedOrganizationRequest.Id" /> field in the body,
///     3. The <see cref="HttpQueryParams.Tenant" /> query string,
///     4. The <see cref="HttpHeaders.Tenant" /> header.
/// </summary>
public class RequestTenantDetective : ITenantDetective
{
    public async Task<Result<TenantDetectionResult, Error>> DetectTenantAsync(HttpContext httpContext,
        Optional<Type> requestDtoType, CancellationToken cancellationToken)
    {
        var shouldHaveTenantId = IsTenantedRequest(requestDtoType, out var type);
        var (found, tenantIdFromRequest) =
            await ParseTenantIdFromRequestAsync(httpContext.Request, type, cancellationToken);
        if (found)
        {
            return new TenantDetectionResult(shouldHaveTenantId, tenantIdFromRequest);
        }

        return new TenantDetectionResult(shouldHaveTenantId, null);
    }

    private static bool IsTenantedRequest(Optional<Type> requestDtoType, out RequestDtoType type)
    {
        type = RequestDtoType.UnTenanted;
        if (!requestDtoType.HasValue)
        {
            return false;
        }

        if (requestDtoType.Value.IsAssignableTo(typeof(ITenantedRequest)))
        {
            type = RequestDtoType.Tenanted;
            return true;
        }

        if (requestDtoType.Value.IsAssignableTo(typeof(IUnTenantedOrganizationRequest)))
        {
            type = RequestDtoType.UnTenantedOrganization;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Attempts to locate the tenant ID from the request query, or header, or body
    /// </summary>
    private static async Task<(bool HasTenantId, string? tenantId)> ParseTenantIdFromRequestAsync(HttpRequest request,
        RequestDtoType type,
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

        if (type.IsTenanted)
        {
            if (request.Query.TryGetValue(nameof(ITenantedRequest.OrganizationId), out var tenantIdFromQueryString))
            {
                var value = GetFirstStringValue(tenantIdFromQueryString);
                if (value.HasValue())
                {
                    return (true, value);
                }
            }
        }

        if (type.IsUnTenantedOrganization)
        {
            if (request.Query.TryGetValue(nameof(IUnTenantedOrganizationRequest.Id), out var tenantIdFromQueryString))
            {
                var value = GetFirstStringValue(tenantIdFromQueryString);
                if (value.HasValue())
                {
                    return (true, value);
                }
            }
        }

        if (type.IsUntenanted)
        {
            if (request.Query.TryGetValue(nameof(ITenantedRequest.OrganizationId), out var tenantIdFromQueryString))
            {
                var value = GetFirstStringValue(tenantIdFromQueryString);
                if (value.HasValue())
                {
                    return (true, value);
                }
            }

            if (request.Query.TryGetValue(nameof(RequestWithTenantIds.TenantId), out var tenantIdFromQueryString2))
            {
                var value = GetFirstStringValue(tenantIdFromQueryString2);
                if (value.HasValue())
                {
                    return (true, value);
                }
            }
        }

        var couldHaveBody = new HttpMethod(request.Method).CanHaveBody();
        if (couldHaveBody)
        {
            var (found, tenantIdFromRequestBody) =
                await ParseTenantIdFromRequestBodyAsync(request, type, cancellationToken);
            if (found)
            {
                return (true, tenantIdFromRequestBody);
            }
        }

        return (false, null);
    }

    private static async Task<(bool HasTenantId, string? tenantId)> ParseTenantIdFromRequestBodyAsync(
        HttpRequest request, RequestDtoType type, CancellationToken cancellationToken)
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
                    await request.ReadFromJsonAsync(typeof(RequestWithTenantIds), cancellationToken);
                request.RewindBody();
                if (requestWithTenantId is RequestWithTenantIds requestWithTenantIds)
                {
                    if (type.IsTenanted)
                    {
                        if (requestWithTenantIds.OrganizationId.HasValue())
                        {
                            return (true, requestWithTenantIds.OrganizationId);
                        }
                    }

                    if (type.IsUnTenantedOrganization)
                    {
                        if (requestWithTenantIds.Id.HasValue())
                        {
                            return (true, requestWithTenantIds.Id);
                        }
                    }

                    if (type.IsUntenanted)
                    {
                        if (requestWithTenantIds.OrganizationId.HasValue())
                        {
                            return (true, requestWithTenantIds.OrganizationId);
                        }

                        if (requestWithTenantIds.TenantId.HasValue())
                        {
                            return (true, requestWithTenantIds.TenantId);
                        }
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

            if (type.IsTenanted)
            {
                if (form.TryGetValue(nameof(ITenantedRequest.OrganizationId), out var tenantId1))
                {
                    var value = GetFirstStringValue(tenantId1);
                    if (value.HasValue())
                    {
                        return (true, value);
                    }
                }
            }

            if (type.IsUnTenantedOrganization)
            {
                if (form.TryGetValue(nameof(IUnTenantedOrganizationRequest.Id), out var tenantId1))
                {
                    var value = GetFirstStringValue(tenantId1);
                    if (value.HasValue())
                    {
                        return (true, value);
                    }
                }
            }

            if (type.IsUntenanted)
            {
                if (form.TryGetValue(nameof(ITenantedRequest.OrganizationId), out var tenantId1))
                {
                    var value = GetFirstStringValue(tenantId1);
                    if (value.HasValue())
                    {
                        return (true, value);
                    }
                }

                if (form.TryGetValue(nameof(RequestWithTenantIds.TenantId), out var tenantId2))
                {
                    var value = GetFirstStringValue(tenantId2);
                    if (value.HasValue())
                    {
                        return (true, value);
                    }
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
    ///     Defines a request that could have a tenant ID within it,
    ///     in any of these properties
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    internal class RequestWithTenantIds : ITenantedRequest, IUnTenantedOrganizationRequest
    {
        public string? TenantId { get; [UsedImplicitly] set; }

        public string? OrganizationId { get; set; }

        public string? Id { get; set; }
    }

    internal class RequestDtoType
    {
        public static readonly RequestDtoType Tenanted = new() { IsTenanted = true };
        public static readonly RequestDtoType UnTenanted = new() { IsUntenanted = true };
        public static readonly RequestDtoType UnTenantedOrganization = new() { IsUnTenantedOrganization = true };

        public bool IsTenanted { get; init; }

        public bool IsUntenanted { get; init; }

        public bool IsUnTenantedOrganization { get; init; }
    }
}