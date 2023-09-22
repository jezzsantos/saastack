namespace Common;

public static class CallContext
{
    public const string UncorrelatedCallId = "uncorrelated";
    public const string UnknownCallerId = "unknown";

    /// <summary>
    ///     Creates an unknown <see cref="ICallContext" />
    /// </summary>
    public static ICallContext CreateUnknown()
    {
        return new UnknownCall();
    }

    /// <summary>
    ///     Creates a custom <see cref="ICallContext" /> with its own call, caller and tenant identifiers
    /// </summary>
    public static ICallContext CreateCustom(string? callId, string callerId, string? tenantId)
    {
        return new CustomCall(callId ?? UncorrelatedCallId, callerId, tenantId);
    }

    private sealed class UnknownCall : CustomCall
    {
        public UnknownCall() : base(UncorrelatedCallId, UnknownCallerId, null)
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