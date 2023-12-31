using Application.Common;
using Application.Interfaces;
using Domain.Common.Authorization;
using Domain.Interfaces;
using Infrastructure.Web.Api.Common;
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

    public string CallerId => CallerConstants.AnonymousUserId;

    public string CallId { get; init; }

    public string? TenantId => null;

    public ICallerContext.CallerRoles Roles => new();

    public ICallerContext.CallerFeatureSets FeatureSets => new(new[] { UserFeatureSets.Basic }, null);

    public string? Authorization => null;

    public bool IsAuthenticated => false;

    public bool IsServiceAccount => false;
}