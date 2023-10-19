using Infrastructure.Common.Recording;

namespace Infrastructure.WebApi.Common;

/// <summary>
///     Defines options for different web hosts
/// </summary>
public class WebHostOptions
{
    public static readonly WebHostOptions BackEndApiHost = new()
    {
        DefaultApiPath = string.Empty,
        AllowCors = true,
        IsMultiTenanted = true,
        Recording = RecorderOptions.BackEndApiHost
    };

    public static readonly WebHostOptions BackEndForFrontEndWebHost = new()
    {
        DefaultApiPath = "api",
        AllowCors = true,
        IsMultiTenanted = false,
        Recording = RecorderOptions.BackEndForFrontEndWebHost
    };

    public static readonly WebHostOptions TestingStubsHost = new()
    {
        DefaultApiPath = string.Empty,
        AllowCors = true,
        IsMultiTenanted = false,
        Recording = RecorderOptions.TestingStubsHost
    };

    private WebHostOptions()
    {
        DefaultApiPath = string.Empty;
        AllowCors = true;
        IsMultiTenanted = false;
        Recording = new RecorderOptions();
    }

    public bool AllowCors { get; private init; }

    public string DefaultApiPath { get; private init; }

    public bool IsMultiTenanted { get; private init; }

    public RecorderOptions Recording { get; private init; }
}