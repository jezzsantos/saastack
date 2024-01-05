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
        Authentication = new AuthenticationOptions
        {
            UsesCookies = false,
            UsesTokens = AuthTokenOptions.Verifies,
            VerifiesHMAC = true
        }
    };
    public new static readonly WebHostOptions BackEndApiHost = new(HostOptions.BackEndApiHost)
    {
        CORS = CORSOption.AnyOrigin,
        TrackApiUsage = true,
        Authentication = new AuthenticationOptions
        {
            UsesCookies = false,
            UsesTokens = AuthTokenOptions.Verifies,
            VerifiesHMAC = true
        }
    };

    public new static readonly WebHostOptions BackEndForFrontEndWebHost = new(HostOptions.BackEndForFrontEndWebHost)
    {
        CORS = CORSOption.SameOrigin,
        TrackApiUsage = false,
        Authentication = new AuthenticationOptions
        {
            UsesCookies = true,
            UsesTokens = AuthTokenOptions.None,
            VerifiesHMAC = false
        }
    };

    public new static readonly WebHostOptions TestingStubsHost = new(HostOptions.TestingStubsHost)
    {
        CORS = CORSOption.AnyOrigin,
        TrackApiUsage = false,
        Authentication = new AuthenticationOptions
        {
            UsesCookies = false,
            UsesTokens = AuthTokenOptions.None,
            VerifiesHMAC = false
        }
    };

    private WebHostOptions(HostOptions options) : base(options)
    {
        CORS = CORSOption.None;
        TrackApiUsage = false;
        Authentication = new AuthenticationOptions();
    }

    public AuthenticationOptions Authentication { get; private init; }

    public CORSOption CORS { get; private init; }

    public bool TrackApiUsage { get; private set; }
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
///     Defines options for handling authentication and authorization
/// </summary>
public class AuthenticationOptions
{
    public bool UsesCookies { get; set; }

    public AuthTokenOptions UsesTokens { get; set; } = AuthTokenOptions.None;

    public bool VerifiesHMAC { get; set; }
}

/// <summary>
///     Defines a primary authentication scheme
/// </summary>
public enum AuthTokenOptions
{
    None = 0,
    Verifies = 1
}