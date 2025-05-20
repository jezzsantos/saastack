namespace Common;

public static class CallContext
{
    /// <summary>
    ///     Creates a custom <see cref="ICallContext" /> with its own call, caller and tenant identifiers
    /// </summary>
    public static ICallContext CreateCustom(Optional<string> callId, string callerId, Optional<string> tenantId,
        DatacenterLocation datacenterLocation)
    {
        return new CustomCall(callId.HasValue
            ? callId.Value
            : CallConstants.UncorrelatedCallId, callerId, tenantId, datacenterLocation);
    }

    /// <summary>
    ///     Creates an unknown <see cref="ICallContext" />
    /// </summary>
    public static ICallContext CreateUnknown(DatacenterLocation region)
    {
        return new UnknownCall(region);
    }

    private sealed class UnknownCall : CustomCall
    {
        public UnknownCall(DatacenterLocation region) : base(CallConstants.UncorrelatedCallId,
            CallConstants.UnknownCallerId,
            Optional<string>.None, region)
        {
        }
    }

    private class CustomCall : ICallContext
    {
        public CustomCall(string callId, string callerId, Optional<string> tenantId, DatacenterLocation region)
        {
            CallId = callId;
            CallerId = callerId;
            TenantId = tenantId;
            HostRegion = region;
        }

        public string CallerId { get; }

        public string CallId { get; }

        public DatacenterLocation HostRegion { get; set; }

        public Optional<string> TenantId { get; }
    }
}