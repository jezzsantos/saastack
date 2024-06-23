using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available platform scoped features
///     (i.e. access to untenanted resources by any end-user)
/// </summary>
public static class PlatformFeatures
{
    public static readonly FeatureLevel
        Basic = new("platform_basic_features"); // Free/Basic limited features, everyone can use
    public static readonly FeatureLevel
        PaidTrial = new("platform_paidtrial_features", Basic); // a.k.a Standard plan features
    public static readonly FeatureLevel
        Paid2 = new("platform_paid2_features", PaidTrial); // a.k.a Professional plan features
    public static readonly FeatureLevel Paid3 = new("platform_paid3_features", Paid2); // a.k.a Enterprise plan features
    public static readonly FeatureLevel TestingOnly = new("platform_testingonly_platform");
    public static readonly FeatureLevel TestingOnlySuperUser = new("platform_testingonly_platform_super", TestingOnly);
    public static readonly Dictionary<string, FeatureLevel> AllFeatures = new()
    {
        { Basic.Name, Basic },
        { PaidTrial.Name, PaidTrial },
        { Paid2.Name, Paid2 },
        { Paid3.Name, Paid3 },
#if TESTINGONLY
        { TestingOnly.Name, TestingOnly },
        { TestingOnlySuperUser.Name, TestingOnlySuperUser },
#endif
    };

    // EXTEND: Add other features to control access to untenanted resources (e.g. untenanted APIs)

    public static readonly IReadOnlyList<FeatureLevel> PlatformAssignableFeatures = new List<FeatureLevel>
    {
        // EXTEND: Add new features that can be assigned/unassigned to EndUsers
        Basic,
        PaidTrial,
        Paid2,
        Paid3,

#if TESTINGONLY
        TestingOnly,
        TestingOnlySuperUser
#endif
    };

    /// <summary>
    ///     Returns the <see cref="FeatureLevel" /> for the specified <see cref="name" /> of the feature
    /// </summary>
    public static FeatureLevel? FindFeatureByName(string name)
    {
#if NETSTANDARD2_0
        return AllFeatures.TryGetValue(name, out var feature)
            ? feature
            : null;
#else
        return AllFeatures.GetValueOrDefault(name);
#endif
    }

    /// <summary>
    ///     Whether the <see cref="feature" /> is assignable
    /// </summary>
    public static bool IsPlatformAssignableFeature(string feature)
    {
        return PlatformAssignableFeatures
            .Select(feat => feat.Name)
            .ContainsIgnoreCase(feature);
    }
}