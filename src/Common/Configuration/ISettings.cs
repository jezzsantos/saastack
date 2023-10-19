namespace Common.Configuration;

/// <summary>
///     Defines a provider of simple settings
/// </summary>
public interface ISettings
{
    public bool IsConfigured { get; }

    public bool GetBool(string key);

    public double GetNumber(string key);

    public string GetString(string key);
}