namespace Common.Configuration;

/// <summary>
///     Configuration settings for the platform, and the current tenancy
/// </summary>
public interface IConfigurationSettings
{
    ISettings Platform { get; }

    ISettings Tenancy { get; }
}