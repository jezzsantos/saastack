using Infrastructure.Common.Recording;

namespace Infrastructure.Hosting.Common;

public class HostOptions
{
    /// <summary>
    ///     Is the host for a group of backend APIs, that will have EventNotifications API and Queues, and also the Ancillary
    ///     API
    /// </summary>
    public static readonly HostOptions BackEndAncillaryApiHost = new("BackendWithAncillaryAPI")
    {
        IsMultiTenanted = true,
        Persistence = new PersistenceOptions
        {
            UsesQueues = true,
            UsesEventing = true
        },
        Recording = RecorderOptions.BackEndAncillaryApiHost
    };

    /// <summary>
    ///     Is the host for a group of backend APIs, that will also have EventNotifications API and Queues
    /// </summary>
    public static readonly HostOptions BackEndApiHost = new("BackendAPI")
    {
        IsMultiTenanted = true,
        Persistence = new PersistenceOptions
        {
            UsesQueues = true,
            UsesEventing = true
        },
        Recording = RecorderOptions.BackEndApiHost
    };

    /// <summary>
    ///     Is the host for the Frontend website BEFFE
    /// </summary>
    public static readonly HostOptions BackEndForFrontEndWebHost = new("FrontendSite")
    {
        IsMultiTenanted = false,
        Persistence = new PersistenceOptions
        {
            UsesQueues = false,
            UsesEventing = false
        },
        Recording = RecorderOptions.BackEndForFrontEndWebHost
    };

    /// <summary>
    ///     Is the host used for testing stubs of 3rd party external services
    /// </summary>
    public static readonly HostOptions TestingStubsHost = new("TestingStubs")
    {
        IsMultiTenanted = false,
        Persistence = new PersistenceOptions
        {
            UsesQueues = false,
            UsesEventing = false
        },
        Recording = RecorderOptions.TestingStubsHost
    };

    protected HostOptions(HostOptions options) : this(options.HostName, options.IsMultiTenanted, options.Persistence,
        options.Recording)
    {
    }

    private HostOptions(string hostName, bool isMultiTenanted, PersistenceOptions persistence,
        RecorderOptions recording) : this(hostName)
    {
        IsMultiTenanted = isMultiTenanted;
        Persistence = persistence;
        Recording = recording;
    }

    private HostOptions(string hostName)
    {
        HostName = hostName;
        IsMultiTenanted = false;
        Persistence = new PersistenceOptions
        {
            UsesQueues = false,
            UsesEventing = false
        };
        Recording = new RecorderOptions();
    }

    public string HostName { get; protected init; }

    public bool IsMultiTenanted { get; protected init; }

    public PersistenceOptions Persistence { get; protected init; }

    public RecorderOptions Recording { get; protected init; }
}