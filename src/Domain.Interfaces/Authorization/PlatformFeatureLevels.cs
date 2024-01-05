using Common.Extensions;

namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines the available feature sets of the product (for un-tenanted/platform resources)
/// </summary>
public static class PlatformFeatureLevels
{
    public static readonly FeatureLevel Basic = new("basic_features"); // Basic features, everyone can use
    public static readonly FeatureLevel Premium = new("prem_features", Basic); // Premium plan features
    public static readonly FeatureLevel Pro = new("pro_features", Premium); // Professional plan features
    public static readonly FeatureLevel TestingOnlyLevel = new("testingonly_user_features");
    public static readonly IReadOnlyList<FeatureLevel> PlatformAssignableFeatureLevels = new List<FeatureLevel>
    {
        // EXTEND: Add new roles that UserAccounts will have, to control access to un-tenanted resources  
        Basic, //Lowest/free tier features, that anyone can use at any time
        Pro, // Lowest/paid tier, trials run on this tier
        Premium, // Highest/paid tier
#if TESTINGONLY
        TestingOnlyLevel
#endif
    };

    /// <summary>
    ///     Whether the <see cref="level" /> is an assignable feature level
    /// </summary>
    public static bool IsPlatformAssignableFeatureLevel(string level)
    {
        return PlatformAssignableFeatureLevels
            .Select(lvl => lvl.Name)
            .ContainsIgnoreCase(level);
    }
}