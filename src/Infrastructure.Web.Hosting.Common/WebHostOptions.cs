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
        Authorization = new AuthorizationOptions
        {
            UsesTokens = true,
            UsesApiKeys = true,
            UsesHMAC = true
        },
        IsBackendForFrontEnd = false
    };
    public new static readonly WebHostOptions BackEndApiHost = new(HostOptions.BackEndApiHost)
    {
        CORS = CORSOption.AnyOrigin,
        Authorization = new AuthorizationOptions
        {
            UsesTokens = true,
            UsesApiKeys = true,
            UsesHMAC = true
        },
        IsBackendForFrontEnd = false
    };

    public new static readonly WebHostOptions BackEndForFrontEndWebHost = new(HostOptions.BackEndForFrontEndWebHost)
    {
        CORS = CORSOption.SameOrigin,
        Authorization = new AuthorizationOptions
        {
            UsesTokens = false,
            UsesApiKeys = false,
            UsesHMAC = false
        },
        IsBackendForFrontEnd = true
    };

    public new static readonly WebHostOptions TestingStubsHost = new(HostOptions.TestingStubsHost)
    {
        CORS = CORSOption.AnyOrigin,
        Authorization = new AuthorizationOptions
        {
            UsesTokens = false,
            UsesApiKeys = false,
            UsesHMAC = false
        },
        IsBackendForFrontEnd = false
    };

    private WebHostOptions(HostOptions options) : base(options)
    {
        CORS = CORSOption.None;
        Authorization = new AuthorizationOptions();
        IsBackendForFrontEnd = false;
    }

    public AuthorizationOptions Authorization { get; private init; }

    public CORSOption CORS { get; private init; }

    public bool IsBackendForFrontEnd { get; set; }
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

/// <summary>
///     Defines options for handling authorization in a host
/// </summary>
public class AuthorizationOptions
{
    public bool HasNone => !UsesApiKeys && !UsesTokens && !UsesHMAC;

    public bool UsesApiKeys { get; set; }

    public bool UsesHMAC { get; set; }

    public bool UsesTokens { get; set; }
}