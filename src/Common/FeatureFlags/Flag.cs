namespace Common.FeatureFlags;

/// <summary>
///     Defines a feature flag.
///     New feature flag values should be added to the <see cref="FeatureFlags.resx" /> file,
///     and they will be source generated into into this class at build time
/// </summary>
#if GENERATORS_COMMON_PROJECT
public class Flag
#else
public partial class Flag
#endif
{
#if TESTINGONLY
    public static readonly Flag TestingOnly = new("testingonly");
#endif

    public Flag(string name)
    {
        Name = name;
    }

    public string Name { get; }
}