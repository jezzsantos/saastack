using Infrastructure.Hosting.Common;

namespace Infrastructure.Web.Hosting.Common;

/// <summary>
///     Defines options for different web hosts
/// </summary>
public class WebHostOptions : HostOptions
{
    /// <inheritdoc cref="HostOptions.BackEndAncillaryApiHost" />
    public new static readonly WebHostOptions BackEndAncillaryApiHost = new(HostOptions.BackEndAncillaryApiHost)
    {
        CORS = CORSOption.AnyOrigin,
        Authorization = new AuthorizationOptions
        {
            UsesTokens = true,
            UsesApiKeys = true,
            UsesHMAC = true
        },
        IsBackendForFrontEnd = false,
        UsesNotifications = true,
        UsesApiDocumentation = true,
        ReceivesWebhooks = true
    };
    /// <inheritdoc cref="HostOptions.BackEndApiHost" />
    public new static readonly WebHostOptions BackEndApiHost = new(HostOptions.BackEndApiHost)
    {
        CORS = CORSOption.AnyOrigin,
        Authorization = new AuthorizationOptions
        {
            UsesTokens = true,
            UsesApiKeys = true,
            UsesHMAC = true
        },
        IsBackendForFrontEnd = false,
        UsesNotifications = true,
        UsesApiDocumentation = true,
        ReceivesWebhooks = false
    };

    /// <inheritdoc cref="HostOptions.BackEndForFrontEndWebHost" />
    public new static readonly WebHostOptions BackEndForFrontEndWebHost = new(HostOptions.BackEndForFrontEndWebHost)
    {
        CORS = CORSOption.SameOrigin,
        Authorization = new AuthorizationOptions
        {
            UsesTokens = false,
            UsesApiKeys = false,
            UsesHMAC = false
        },
        IsBackendForFrontEnd = true,
        UsesNotifications = false,
        UsesApiDocumentation = true,
        ReceivesWebhooks = false
    };

    /// <inheritdoc cref="HostOptions.TestingStubsHost" />
    public new static readonly WebHostOptions TestingStubsHost = new(HostOptions.TestingStubsHost)
    {
        CORS = CORSOption.AnyOrigin,
        Authorization = new AuthorizationOptions
        {
            UsesTokens = false,
            UsesApiKeys = false,
            UsesHMAC = false
        },
        IsBackendForFrontEnd = false,
        UsesNotifications = false,
        UsesApiDocumentation = false,
        ReceivesWebhooks = false
    };

    private WebHostOptions(HostOptions options) : base(options)
    {
        CORS = CORSOption.None;
        Authorization = new AuthorizationOptions();
        IsBackendForFrontEnd = false;
    }

    public AuthorizationOptions Authorization { get; private init; }

    public CORSOption CORS { get; private init; }

    public string HostVersion { get; set; } = "v1";

    public bool IsBackendForFrontEnd { get; set; }

    public bool ReceivesWebhooks { get; set; }

    public bool UsesApiDocumentation { get; set; }

    public bool UsesNotifications { get; set; }
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