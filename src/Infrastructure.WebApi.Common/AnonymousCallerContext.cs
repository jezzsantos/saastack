using Application.Common;
using Application.Interfaces;
using Domain.Common;
using Domain.Interfaces;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     A <see cref="ICallerContext" /> that contains the anonymous user context
/// </summary>
public class AnonymousCallerContext : ICallerContext
{
    public string CallerId => CallerConstants.AnonymousUserId;

    public string CallId => Caller.GenerateCallId();

    public string? TenantId => null;

    public ICallerContext.CallerRoles Roles => new();

    public ICallerContext.CallerFeatureSets FeatureSets => new(new[] { UserFeatureSets.Core }, null);

    public string? Authorization => null;

    public bool IsAuthenticated => false;

    public bool IsServiceAccount => false;
}