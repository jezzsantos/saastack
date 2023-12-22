using Application.Interfaces;

namespace Infrastructure.Common.Recording;

/// <summary>
///     Defines the options for recording in the different environments
/// </summary>
public class RecorderOptions
{
    public static readonly RecorderOptions BackEndApiHost = new()
    {
        TrackUsageOfAllApis = true,
        UsageComponentName = UsageConstants.Components.BackEndApiHost,
        Testing = new RecordingEnvironmentOptions
        {
            CrashReporting = CrashReporterOption.None,
            AuditReporting = AuditReporterOption.ReliableQueue,
            MetricReporting = MetricReporterOption.None,
            UsageReporting = UsageReporterOption.ReliableQueue
        },
        Production = new RecordingEnvironmentOptions
        {
            CrashReporting = CrashReporterOption.Cloud,
            AuditReporting = AuditReporterOption.ReliableQueue,
            MetricReporting = MetricReporterOption.Cloud,
            UsageReporting = UsageReporterOption.ReliableQueue
        }
    };

    public static readonly RecorderOptions BackEndForFrontEndWebHost = new()
    {
        TrackUsageOfAllApis = false,
        UsageComponentName = UsageConstants.Components.BackEndForFrontEndWebHost,
        Testing = new RecordingEnvironmentOptions
        {
            CrashReporting = CrashReporterOption.None,
            AuditReporting = AuditReporterOption.None,
            MetricReporting = MetricReporterOption.ForwardToBackEnd,
            UsageReporting = UsageReporterOption.ForwardToBackEnd
        },
        Production = new RecordingEnvironmentOptions
        {
            CrashReporting = CrashReporterOption.Cloud,
            AuditReporting = AuditReporterOption.None,
            MetricReporting = MetricReporterOption.ForwardToBackEnd,
            UsageReporting = UsageReporterOption.ForwardToBackEnd
        }
    };

    public static readonly RecorderOptions TestingStubsHost = new()
    {
        TrackUsageOfAllApis = false,
        UsageComponentName = "TestingStubApiHost",
        Testing = new RecordingEnvironmentOptions
        {
            CrashReporting = CrashReporterOption.None,
            AuditReporting = AuditReporterOption.None,
            MetricReporting = MetricReporterOption.None,
            UsageReporting = UsageReporterOption.None
        },
        Production = new RecordingEnvironmentOptions
        {
            CrashReporting = CrashReporterOption.None,
            AuditReporting = AuditReporterOption.None,
            MetricReporting = MetricReporterOption.None,
            UsageReporting = UsageReporterOption.None
        }
    };

    public RecordingEnvironmentOptions CurrentEnvironment
    {
        get
        {
#if TESTINGONLY
            return Testing;
#else
                return Production;
#endif
        }
    }

    public RecordingEnvironmentOptions Production { get; private set; } = new();

    public RecordingEnvironmentOptions Testing { get; private set; } = new();

    public bool TrackUsageOfAllApis { get; private set; }

    public string UsageComponentName { get; private set; } = string.Empty;
}

/// <summary>
///     Defines recording options for a specific environment
/// </summary>
public class RecordingEnvironmentOptions
{
    public AuditReporterOption AuditReporting { get; set; } = AuditReporterOption.None;

    public CrashReporterOption CrashReporting { get; set; } = CrashReporterOption.None;

    public MetricReporterOption MetricReporting { get; set; } = MetricReporterOption.None;

    public UsageReporterOption UsageReporting { get; set; } = UsageReporterOption.None;
}

/// <summary>
///     Defines types of Crash Reporters
/// </summary>
public enum CrashReporterOption
{
    None = 0,
    Cloud = 1
}

/// <summary>
///     Defines types of Audit Reporters
/// </summary>
public enum AuditReporterOption
{
    None = 0,
    ReliableQueue = 1
}

/// <summary>
///     Defines types of Metric Reporters
/// </summary>
public enum MetricReporterOption
{
    None = 0,
    Cloud = 1,
    ForwardToBackEnd = 3
}

/// <summary>
///     Defines types of Usage Reporters
/// </summary>
public enum UsageReporterOption
{
    None = 0,
    ReliableQueue = 1,
    ForwardToBackEnd = 2
}