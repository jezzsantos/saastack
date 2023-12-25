using Infrastructure.Hosting.Common;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Defines options for different web hosts
/// </summary>
public class WebHostOptions : HostOptions
{
    public new static readonly WebHostOptions BackEndAncillaryApiHost = new(HostOptions.BackEndAncillaryApiHost)
    {
        DefaultApiPath = string.Empty,
        AllowCors = true,
        TrackApiUsage = true,
    };
    public new static readonly WebHostOptions BackEndApiHost = new(HostOptions.BackEndApiHost)
    {
        DefaultApiPath = string.Empty,
        AllowCors = true,
        TrackApiUsage = true
    };

    public new static readonly WebHostOptions BackEndForFrontEndWebHost = new(HostOptions.BackEndForFrontEndWebHost)
    {
        DefaultApiPath = "api",
        AllowCors = true,
        TrackApiUsage = false
    };

    public new static readonly WebHostOptions TestingStubsHost = new(HostOptions.TestingStubsHost)
    {
        DefaultApiPath = string.Empty,
        AllowCors = true,
        TrackApiUsage = false
    };

    private WebHostOptions(HostOptions options) : base(options)
    {
        DefaultApiPath = string.Empty;
        AllowCors = true;
        TrackApiUsage = false;
    }
    
    public bool TrackApiUsage { get; private set; }

    public bool AllowCors { get; private init; }

    public string DefaultApiPath { get; private init; }
}