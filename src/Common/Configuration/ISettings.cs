namespace Common.Configuration;

/// <summary>
///     Defines a provider of simple settings
/// </summary>
public interface ISettings
{
    public bool IsConfigured { get; }

    public bool GetBool(string key, bool? defaultValue = null);

    public double GetNumber(string key, double? defaultValue = null);

    public string GetString(string key, string? defaultValue = null);
}