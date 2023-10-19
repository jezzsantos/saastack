using Infrastructure.Interfaces;

namespace Infrastructure.Common;

/// <summary>
///     Defines a simple tenancy context that can be set
/// </summary>
public class SimpleTenancyContext : ITenancyContext
{
    public string? Current { get; private set; }

    public IReadOnlyDictionary<string, object> Settings { get; private set; } = new Dictionary<string, object>();

    public void Set(string id, Dictionary<string, object> settings)
    {
        Current = id;
        Settings = settings;
    }
}