namespace Domain.Common.Authorization;

/// <summary>
///     Defines the available feature sets of the product (features for un-tenanted resources)
/// </summary>
public static class UserFeatureSets
{
    // EXTEND: Add new feature sets
    public const string Basic = "basic_features"; // Free features, everyone can use
    public const string Premium = "prem_features"; // Premium plan features
    public const string Pro = "pro_features"; // Professional plan features

#if TESTINGONLY
    public const string TestingOnlyFeatures = "testingonly_user_features";
#endif

    public static readonly IReadOnlyList<string> AssignableFeatureSets = new List<string>
    {
        // EXTEND: Add new roles that UserAccounts will have, to control access to un-tenanted resources  
        Basic,
        Pro,
        Premium,
#if TESTINGONLY
        TestingOnlyFeatures
#endif
    };
}