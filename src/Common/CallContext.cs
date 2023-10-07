namespace Common;

public static class CallContext
{
    /// <summary>
    ///     Creates a custom <see cref="ICallContext" /> with its own call, caller and tenant identifiers
    /// </summary>
    public static ICallContext CreateCustom(string? callId, string callerId, string? tenantId)
    {
        return new CustomCall(callId ?? CallConstants.UncorrelatedCallId, callerId, tenantId);
    }

    /// <summary>
    ///     Creates an unknown <see cref="ICallContext" />
    /// </summary>
    public static ICallContext CreateUnknown()
    {
        return new UnknownCall();
    }

    private sealed class UnknownCall : CustomCall
    {
        public UnknownCall() : base(CallConstants.UncorrelatedCallId, CallConstants.UnknownCallerId, null)
        {
        }
    }

    private class CustomCall : ICallContext
    {
        public CustomCall(string callId, string callerId, string? tenantId)
        {
            CallId = callId;
            CallerId = callerId;
            TenantId = tenantId;
        }

        public string? TenantId { get; }

        public string CallerId { get; }

        public string CallId { get; }
    }
}