using Application.Interfaces;
using Common;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;

namespace Application.Common;

public static class Caller
{
    /// <summary>
    ///     Returns a caller used to represent an authenticated caller with no access
    /// </summary>
    public static ICallerContext CreateAsAnonymous(DatacenterLocation region)
    {
        return new AnonymousCaller(Optional<string>.None, region);
    }

    /// <summary>
    ///     Returns a caller used to represent an authenticated caller with no access
    /// </summary>
    public static ICallerContext CreateAsAnonymousTenant(string tenantId, DatacenterLocation region)
    {
        return new AnonymousCaller(tenantId, region);
    }

    /// <summary>
    ///     Returns a caller used to represent the caller represented by the given <see cref="ICallContext" />
    /// </summary>
    public static ICallerContext CreateAsCallerFromCall(ICallContext call)
    {
        return new CustomCaller(call);
    }

    /// <summary>
    ///     Returns a caller used to represent inbound webhook calls from 3rd party integrations
    /// </summary>
    public static ICallerContext CreateAsExternalWebHook(string callId, DatacenterLocation region)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        return new ExternalWebHookAccountCaller(callId, region);
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenance(DatacenterLocation region)
    {
        return new MaintenanceAccountCaller(Optional<string>.None, Optional<string>.None, region);
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenance(string callId, DatacenterLocation region)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        return new MaintenanceAccountCaller(callId, Optional<string>.None, region);
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenance(ICallerContext caller)
    {
        return new MaintenanceAccountCaller(caller.CallId, Optional<string>.None, caller.HostRegion);
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenanceTenant(string callId, Optional<string> tenantId,
        DatacenterLocation region)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        return new MaintenanceAccountCaller(callId, tenantId, region);
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenanceTenant(string tenantId, DatacenterLocation region)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        return new MaintenanceAccountCaller(null, tenantId, region);
    }

    /// <summary>
    ///     Returns a caller used for calling 3rd party [external] services
    /// </summary>
    public static ICallerContext CreateAsServiceClient(DatacenterLocation region)
    {
        return new ServiceClientAccountCaller(region);
    }

    /// <summary>
    ///     Returns a newly generated ID for the call
    /// </summary>
    public static string GenerateCallId()
    {
        return $"{Guid.NewGuid():N}";
    }

    /// <summary>
    ///     An unauthenticated account (on the current tenant) with no roles or access
    /// </summary>
    private sealed class AnonymousCaller : ICallerContext
    {
        public AnonymousCaller(Optional<string> tenantId, DatacenterLocation region)
        {
            TenantId = tenantId;
            Roles = new ICallerContext.CallerRoles();
            Features = new ICallerContext.CallerFeatures([PlatformFeatures.Basic], null);
            HostRegion = region;
        }

        public Optional<ICallerContext.CallerAuthorization> Authorization =>
            Optional<ICallerContext.CallerAuthorization>.None;

        public string CallerId => CallerConstants.AnonymousUserId;

        public string CallId => GenerateCallId();

        public ICallerContext.CallerFeatures Features { get; }

        public DatacenterLocation HostRegion { get; }

        public bool IsAuthenticated => false;

        public bool IsServiceAccount => false;

        public ICallerContext.CallerRoles Roles { get; }

        public Optional<string> TenantId { get; }
    }

    /// <summary>
    ///     An authenticated service account used internally for processing (e.g. relaying domain event notifications)
    /// </summary>
    private sealed class MaintenanceAccountCaller : ICallerContext
    {
        public MaintenanceAccountCaller(Optional<string> callId, Optional<string> tenantId, DatacenterLocation region)
        {
            CallId = callId.HasValue
                ? callId.Value
                : GenerateCallId();
            TenantId = tenantId;
            Roles = new ICallerContext.CallerRoles([PlatformRoles.ServiceAccount], null);
            Features =
                new ICallerContext.CallerFeatures(
                    [PlatformFeatures.Paid2], null);
            HostRegion = region;
        }

        public Optional<ICallerContext.CallerAuthorization> Authorization =>
            Optional<ICallerContext.CallerAuthorization>.None;

        public string CallerId => CallerConstants.MaintenanceAccountUserId;

        public string CallId { get; }

        public ICallerContext.CallerFeatures Features { get; }

        public DatacenterLocation HostRegion { get; }

        public bool IsAuthenticated => true;

        public bool IsServiceAccount => CallerConstants.IsServiceAccount(CallerId);

        public ICallerContext.CallerRoles Roles { get; }

        public Optional<string> TenantId { get; }
    }

    /// <summary>
    ///     An authenticated service account used to call out to 3rd party [external] services
    /// </summary>
    private sealed class ServiceClientAccountCaller : ICallerContext
    {
        public ServiceClientAccountCaller(DatacenterLocation region)
        {
            HostRegion = region;
        }

        public Optional<ICallerContext.CallerAuthorization> Authorization =>
            Optional<ICallerContext.CallerAuthorization>.None;

        public string CallerId => CallerConstants.ServiceClientAccountUserId;

        public string CallId => GenerateCallId();

        public ICallerContext.CallerFeatures Features { get; } = new(
            [PlatformFeatures.Paid2], null);

        public DatacenterLocation HostRegion { get; }

        public bool IsAuthenticated => true;

        public bool IsServiceAccount => CallerConstants.IsServiceAccount(CallerId);

        public ICallerContext.CallerRoles Roles { get; } = new([PlatformRoles.ServiceAccount], null);

        public Optional<string> TenantId => null;
    }

    /// <summary>
    ///     An authenticated service account used to represent inbound webhook calls from 3rd party integrations
    /// </summary>
    private sealed class ExternalWebHookAccountCaller : ICallerContext
    {
        public ExternalWebHookAccountCaller(Optional<string> callId, DatacenterLocation region)
        {
            CallId = callId.HasValue
                ? callId.Value
                : GenerateCallId();
            Roles = new ICallerContext.CallerRoles([PlatformRoles.ServiceAccount], null);
            Features = new ICallerContext.CallerFeatures([PlatformFeatures.Basic], null);
            HostRegion = region;
        }

        public Optional<ICallerContext.CallerAuthorization> Authorization =>
            Optional<ICallerContext.CallerAuthorization>.None;

        public string CallerId => CallerConstants.ExternalWebhookAccountUserId;

        public string CallId { get; }

        public ICallerContext.CallerFeatures Features { get; }

        public DatacenterLocation HostRegion { get; }

        public bool IsAuthenticated => true;

        public bool IsServiceAccount => CallerConstants.IsServiceAccount(CallerId);

        public ICallerContext.CallerRoles Roles { get; }

        public Optional<string> TenantId => null;
    }

    /// <summary>
    ///     An unauthenticated account with no roles or access
    /// </summary>
    private sealed class CustomCaller : ICallerContext
    {
        public CustomCaller(ICallContext call)
        {
            CallerId = call.CallerId;
            CallId = call.CallId;
            TenantId = call.TenantId;
            Roles = new ICallerContext.CallerRoles();
            Features = new ICallerContext.CallerFeatures([PlatformFeatures.Basic], null);
            HostRegion = call.HostRegion;
        }

        public Optional<ICallerContext.CallerAuthorization> Authorization =>
            Optional<ICallerContext.CallerAuthorization>.None;

        public string CallerId { get; }

        public string CallId { get; }

        public ICallerContext.CallerFeatures Features { get; }

        public DatacenterLocation HostRegion { get; }

        public bool IsAuthenticated => false;

        public bool IsServiceAccount => false;

        public ICallerContext.CallerRoles Roles { get; }

        public Optional<string> TenantId { get; }
    }
}