using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available feature levels of the product (for tenanted resources)
/// </summary>
public static class MemberFeatureLevels
{
    public static readonly FeatureLevel Basic = PlatformFeatureLevels.Basic; // Basic features, everyone can use
    public static readonly FeatureLevel Premium = PlatformFeatureLevels.Premium; // Premium plan features
    public static readonly FeatureLevel Pro = PlatformFeatureLevels.Pro; // Professional plan features
    public static readonly FeatureLevel TestingOnlyFeatures = PlatformFeatureLevels.TestingOnlyLevel;
    public static readonly IReadOnlyList<FeatureLevel> MemberAssignableFeatureLevels = new List<FeatureLevel>
    {
        // EXTEND: Add new features that Memberships will have, to control access to tenanted resources  
        Basic, //Lowest/free tier features, that anyone can use at any time
        Pro, // Lowest/paid tier, trials run on this tier
        Premium, // Highest/paid tier
#if TESTINGONLY
        TestingOnlyFeatures
#endif
    };

    /// <summary>
    ///     Whether the <see cref="level" /> is an assignable feature level
    /// </summary>
    public static bool IsMemberAssignableFeatureLevel(string level)
    {
        return MemberAssignableFeatureLevels
            .Select(lvl => lvl.Name)
            .ContainsIgnoreCase(level);
    }
}