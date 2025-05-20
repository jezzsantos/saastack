using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service that provides information about the physical region (or DataCenter) in which this host is running
/// </summary>
public interface IHostRegionService
{
    /// <summary>
    ///     Returns the region that this host is running in
    /// </summary>
    public DatacenterLocation GetRegion();
}