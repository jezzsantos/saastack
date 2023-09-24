using Application.Interfaces;
using Common;
using Domain.Common;
using Domain.Interfaces;

namespace Application.Common;

public static class Caller
{
    /// <summary>
    ///     Returns a caller used to represent an authenticated caller with no access
    /// </summary>
    public static ICallerContext CreateAsAnonymous()
    {
        return new AnonymousCaller();
    }

    /// <summary>
    ///     Returns a caller used to represent an authenticated caller with no access
    /// </summary>
    public static ICallerContext CreateAsAnonymousTenant(string tenantId)
    {
        return new AnonymousCaller(tenantId);
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
    public static ICallerContext CreateAsExternalWebHook(string callId)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        return new ExternalWebHookAccountCaller(callId);
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenance()
    {
        return new MaintenanceAccountCaller();
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenance(string callId)
    {
        ArgumentException.ThrowIfNullOrEmpty(callId);
        return new MaintenanceAccountCaller(callId);
    }

    /// <summary>
    ///     Returns a caller used for internal processing (e.g. raising domain event notifications)
    /// </summary>
    public static ICallerContext CreateAsMaintenanceTenant(string tenantId)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantId);
        return new MaintenanceAccountCaller(null, tenantId);
    }

    /// <summary>
    ///     Returns a caller used for calling 3rd party [external] services
    /// </summary>
    public static ICallerContext CreateAsServiceClient()
    {
        return new ServiceClientAccountCaller();
    }

    public static string GenerateCallId()
    {
        return $"{Guid.NewGuid():N}";
    }

    /// <summary>
    ///     An unauthenticated account (on the current tenant) with no roles or access
    /// </summary>
    private sealed class AnonymousCaller : ICallerContext
    {
        public AnonymousCaller(string? tenantId = null)
        {
            TenantId = tenantId;
            Roles = new ICallerContext.CallerRoles();
            FeatureSets = new ICallerContext.CallerFeatureSets(new[] { UserFeatureSets.Core }, null);
        }

        public ICallerContext.CallerRoles Roles { get; }

        public ICallerContext.CallerFeatureSets FeatureSets { get; }

        public string? Authorization => null;

        public bool IsAuthenticated => false;

        public bool IsServiceAccount => false;

        public string CallId => GenerateCallId();

        public string CallerId => CallerConstants.AnonymousUserId;

        public string? TenantId { get; }
    }

    /// <summary>
    ///     An authenticated service account used internally for processing (e.g. relaying domain event notifications)
    /// </summary>
    private sealed class MaintenanceAccountCaller : ICallerContext
    {
        public MaintenanceAccountCaller(string? callId = null, string? tenantId = null)
        {
            CallId = callId ?? GenerateCallId();
            TenantId = tenantId;
            Roles = new ICallerContext.CallerRoles(new[] { UserRoles.ServiceAccount }, null);
            FeatureSets =
                new ICallerContext.CallerFeatureSets(
                    new[] { UserFeatureSets.Core, UserFeatureSets.Basic, UserFeatureSets.Premium }, null);
        }

        public ICallerContext.CallerRoles Roles { get; }

        public ICallerContext.CallerFeatureSets FeatureSets { get; }

        public string? Authorization => null;

        public bool IsAuthenticated => true;

        public bool IsServiceAccount => CallerConstants.IsServiceAccount(CallerId);

        public string CallId { get; }

        public string CallerId => CallerConstants.MaintenanceAccountUserId;

        public string? TenantId { get; }
    }

    /// <summary>
    ///     An authenticated service account used to call out to 3rd party [external] services
    /// </summary>
    private sealed class ServiceClientAccountCaller : ICallerContext
    {
        public ICallerContext.CallerRoles Roles { get; } = new(new[] { UserRoles.ServiceAccount }, null);

        public ICallerContext.CallerFeatureSets FeatureSets { get; } = new(
            new[] { UserFeatureSets.Core, UserFeatureSets.Basic, UserFeatureSets.Premium }, null);

        public string? Authorization => null;

        public bool IsAuthenticated => true;

        public bool IsServiceAccount => CallerConstants.IsServiceAccount(CallerId);

        public string CallId => GenerateCallId();

        public string CallerId => CallerConstants.ServiceClientAccountUserId;

        public string? TenantId => null;
    }

    /// <summary>
    ///     An authenticated service account used to represent inbound webhook calls from 3rd party integrations
    /// </summary>
    private sealed class ExternalWebHookAccountCaller : ICallerContext
    {
        public ExternalWebHookAccountCaller(string? callId = null)
        {
            CallId = callId ?? GenerateCallId();
            Roles = new ICallerContext.CallerRoles(new[] { UserRoles.ServiceAccount }, null);
            FeatureSets =
                new ICallerContext.CallerFeatureSets(
                    new[] { UserFeatureSets.Core }, null);
        }

        public ICallerContext.CallerRoles Roles { get; }

        public ICallerContext.CallerFeatureSets FeatureSets { get; }

        public string? Authorization => null;

        public bool IsAuthenticated => true;

        public bool IsServiceAccount => CallerConstants.IsServiceAccount(CallerId);

        public string CallId { get; }

        public string CallerId => CallerConstants.ExternalWebhookAccountUserId;

        public string? TenantId => null;
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
            FeatureSets = new ICallerContext.CallerFeatureSets(new[] { UserFeatureSets.Core }, null);
        }

        public ICallerContext.CallerRoles Roles { get; }

        public ICallerContext.CallerFeatureSets FeatureSets { get; }

        public string? Authorization => null;

        public bool IsAuthenticated => false;

        public bool IsServiceAccount => false;

        public string CallId { get; }

        public string CallerId { get; }

        public string? TenantId { get; }
    }
}