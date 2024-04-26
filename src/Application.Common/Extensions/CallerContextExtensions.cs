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
    public static ICallContext ToCall(this ICallerContext? context)
    {
        if (context.NotExists())
        {
            return CallContext.CreateUnknown();
        }

        return CallContext.CreateCustom(context.CallId, context.CallerId, context.TenantId);
    }

    /// <summary>
    ///     Returns the caller identifier from the context
    /// </summary>
    public static Identifier ToCallerId(this ICallerContext caller)
    {
        return caller.CallerId.ToId();
    }
}