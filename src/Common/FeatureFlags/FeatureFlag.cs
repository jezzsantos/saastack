namespace Common.FeatureFlags;

/// <summary>
///     The definition of a feature flag
/// </summary>
public class FeatureFlag
{
    public bool IsEnabled { get; set; }

    public required string Name { get; set; }
}