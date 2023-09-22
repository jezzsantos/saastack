namespace Domain.Common;

/// <summary>
///     Defines the available feature sets of the product (features for un-tenanted resources)
/// </summary>
public static class UserFeatureSets
{
    // EXTEND: Add new feature sets
    public const string Core = "core_features";
    public const string Basic = "basic_features";
    public const string Premium = "premium_features";

#if TESTINGONLY
    public const string TestingOnlyFeatures = "testingonly_user_features";
#endif

    public static readonly IReadOnlyList<string> AssignableFeatureSets = new List<string>
    {
        // EXTEND: Add new roles that UserAccounts will have, to control access to un-tenanted resources  
        Core,
        Basic,
        Premium,
#if TESTINGONLY
        TestingOnlyFeatures
#endif
    };
}