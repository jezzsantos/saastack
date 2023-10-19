namespace Infrastructure.Interfaces;

/// <summary>
///     Defines the context of a tenancy operating on the platform
/// </summary>
public interface ITenancyContext
{
    string? Current { get; }

    public IReadOnlyDictionary<string, object> Settings { get; }

    void Set(string id, Dictionary<string, object> settings);
}