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
        public const string Id = "ResourceId";
        public const string UsedById = "UserId";
        public const string TenantId = "TenantId";
        public const string CallId = "CallId";
        public const string Component = "Component";
        public const string AuditCode = "Code";
        public const string MetricEventName = "Metric";
    }

    public static class Events
    {
        public static class UsageScenarios
        {
            public const string Audit = "Audited";
            public const string Measurement = "Measured";
        }
    }
}