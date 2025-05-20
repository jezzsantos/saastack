using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;

namespace Application.Common.Extensions;

public static class CallerContextExtensions
{
    /// <summary>
    ///     Returns the call context from the context
    /// </summary>
    public static ICallContext ToCall(this ICallerContext? caller)
    {
        if (caller.NotExists())
        {
            return CallContext.CreateUnknown(DatacenterLocations.Unknown);
        }

        return CallContext.CreateCustom(caller.CallId, caller.CallerId, caller.TenantId, caller.HostRegion);
    }

    /// <summary>
    ///     Returns the caller identifier from the context
    /// </summary>
    public static Identifier ToCallerId(this ICallerContext caller)
    {
        return caller.CallerId.ToId();
    }
}