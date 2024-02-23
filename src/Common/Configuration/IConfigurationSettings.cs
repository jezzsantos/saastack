namespace Common.Configuration;

/// <summary>
///     Configuration settings for the platform, and the current tenancy
/// </summary>
public interface IConfigurationSettings : ISettings
{
    /// <summary>
    ///     Returns settings used by the platform
    /// </summary>
    ISettings Platform { get; }

    /// <summary>
    ///     Returns settings used by the current tenancy
    /// </summary>
    ISettings Tenancy { get; }
}