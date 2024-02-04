using Infrastructure.Common.Recording;

namespace Infrastructure.Hosting.Common;

public class HostOptions
{
    public static readonly HostOptions BackEndAncillaryApiHost = new("BackendWithAncillaryAPI")
    {
        IsMultiTenanted = false, //TODO: change for multi-tenanted
        Persistence = new PersistenceOptions
        {
            UsesQueues = true,
            UsesEventing = true
        },
        Recording = RecorderOptions.BackEndAncillaryApiHost
    };
    public static readonly HostOptions BackEndApiHost = new("BackendAPI")
    {
        IsMultiTenanted = false, //TODO: change for multi-tenanted
        Persistence = new PersistenceOptions
        {
            UsesQueues = true,
            UsesEventing = true
        },
        Recording = RecorderOptions.BackEndApiHost
    };

    public static readonly HostOptions BackEndForFrontEndWebHost = new("FrontendSite")
    {
        IsMultiTenanted = false, //TODO: change for multi-tenanted
        Persistence = new PersistenceOptions
        {
            UsesQueues = false,
            UsesEventing = false
        },
        Recording = RecorderOptions.BackEndForFrontEndWebHost
    };

    public static readonly HostOptions TestingStubsHost = new("TestingStubs")
    {
        IsMultiTenanted = false, //TODO: change for multi-tenanted
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