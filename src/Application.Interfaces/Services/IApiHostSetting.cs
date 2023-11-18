namespace Application.Interfaces.Services;

/// <summary>
///     Defines settings for the current API host
/// </summary>
public interface IApiHostSetting
{
    /// <summary>
    ///     Returns the URL for the Ancillary API host
    /// </summary>
    string GetAncillaryApiHostBaseUrl();

    /// <summary>
    ///     Returns the URL of the Website host
    /// </summary>
    string GetWebsiteHostBaseUrl();
}