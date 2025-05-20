using Application.Common;
using Application.Interfaces;
using Common;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Infrastructure.Web.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     A <see cref="ICallerContext" /> that contains the anonymous user context
/// </summary>
public class AnonymousCallerContext : ICallerContext
{
    public AnonymousCallerContext(IHttpContextAccessor httpContext)
    {
        CallId = httpContext.HttpContext!.Items.TryGetValue(RequestCorrelationFilter.CorrelationIdItemName,
            out var callId)
            ? callId!.ToString()!
            : Caller.GenerateCallId();
    }

    public Optional<ICallerContext.CallerAuthorization> Authorization =>
        Optional<ICallerContext.CallerAuthorization>.None;

    public string CallerId => CallerConstants.AnonymousUserId;

    public string CallId { get; init; }

    public ICallerContext.CallerFeatures Features => new(new[] { PlatformFeatures.Basic }, null);

    public DatacenterLocation HostRegion => DatacenterLocations.Local;

    public bool IsAuthenticated => false;

    public bool IsServiceAccount => false;

    public ICallerContext.CallerRoles Roles => new();

    public Optional<string> TenantId => null;
}