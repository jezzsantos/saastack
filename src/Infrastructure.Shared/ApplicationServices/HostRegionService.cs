using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a service that returns the physical region (or DataCenter) in which this host is running.
/// </summary>
public class HostRegionService : IHostRegionService
{
    private const string RegionSettingName = "Hosts:ThisHost:Region";
    private readonly Region _region;

    public HostRegionService(IConfigurationSettings settings)
    {
        _region = settings.GetString(RegionSettingName).ToEnum<Region>();
    }

    public Region GetRegion()
    {
        return _region;
    }
}