namespace Application.Interfaces;

/// <summary>
///     Values used when reporting usages
/// </summary>
public static class UsageConstants
{
    public static class Components
    {
        public const string BackEndApiHost = "API";
        public const string BackEndForFrontEndWebHost = "WEB";
    }

    public static class Properties
    {
        public const string AuditCode = "Code";
        public const string CallId = "CallId";
        public const string Component = "Component";
        public const string Duration = "Duration";
        public const string EndPoint = "EndPoint";
        public const string Id = "ResourceId";
        public const string IpAddress = "IpAddress";
        public const string MetricEventName = "Metric";
        public const string Path = "Path";
        public const string ReferredBy = "ReferredBy";
        public const string Started = "Started";
        public const string TenantId = "TenantId";
        public const string Timestamp = "Timestamp";
        public const string UsedById = "UserId";
        public const string ResourceId = "ResourceId";
        public const string UserAgent = "UserAgent";
        public const string HttpRoute = "Route";
        public const string HttpMethod = "Method";
        public const string HttpPath = "Path";
        public const string HttpStatusCode = "Status";
    }

    public static class Events
    {
        public static class UsageScenarios
        {
            public const string Audit = "Audited";
            public const string BookingCancelled = "Booking Cancelled";
            public const string BookingCreated = "Booking Created";
            public const string MachineRegistered = "Machine Registered";
            public const string Measurement = "Measured";
            public const string PersonRegistrationConfirmed = "User Registered";
            public const string PersonRegistrationCreated = "User Registration Created";
        }

        public static class Web
        {
            public const string WebPageVisit = "web.page.visit";
        }

        public static class Api
        {
            public const string HttpRequestRequested = "http.request";
            public const string HttpRequestResponded = "http.response";
        }
    }
}