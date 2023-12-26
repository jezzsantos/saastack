using Infrastructure.Hosting.Common;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Defines options for different web hosts
/// </summary>
public class WebHostOptions : HostOptions
{
    public new static readonly WebHostOptions BackEndAncillaryApiHost = new(HostOptions.BackEndAncillaryApiHost)
    {
        CORS = CORSOption.AnyOrigin,
        TrackApiUsage = true,
        UsesAuth = true
    };
    public new static readonly WebHostOptions BackEndApiHost = new(HostOptions.BackEndApiHost)
    {
        CORS = CORSOption.AnyOrigin,
        TrackApiUsage = true,
        UsesAuth = true
    };

    public new static readonly WebHostOptions BackEndForFrontEndWebHost = new(HostOptions.BackEndForFrontEndWebHost)
    {
        CORS = CORSOption.SameOrigin,
        TrackApiUsage = false,
        UsesAuth = false
    };

    public new static readonly WebHostOptions TestingStubsHost = new(HostOptions.TestingStubsHost)
    {
        CORS = CORSOption.AnyOrigin,
        TrackApiUsage = false,
        UsesAuth = false
    };

    private WebHostOptions(HostOptions options) : base(options)
    {
        CORS = CORSOption.None;
        TrackApiUsage = false;
    }

    public CORSOption CORS { get; private init; }

    public bool TrackApiUsage { get; private set; }

    public bool UsesAuth { get; private init; }
}

/// <summary>
///     Defines a CORS option
/// </summary>
public enum CORSOption
{
    None = 0,
    SameOrigin = 1,
    AnyOrigin = 2
}