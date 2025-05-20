using Application.Services.Shared;
using Common;
using Common.Configuration;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a service that returns the physical region (or DataCenter) in which this host is running.
/// </summary>
public class HostRegionService : IHostRegionService
{
    private const string RegionSettingName = "Hosts:ThisHost:Region";
    private readonly DatacenterLocation _datacenterLocation;

    public HostRegionService(IConfigurationSettings settings)
    {
        _datacenterLocation = DatacenterLocations.FindOrDefault(settings.GetString(RegionSettingName));
    }

    public DatacenterLocation GetRegion()
    {
        return _datacenterLocation;
    }
}