namespace Domain.Common.Authorization;

/// <summary>
///     Defines the available feature sets of the product (features for tenanted resources)
/// </summary>
public static class OrganizationFeatureSets
{
    // EXTEND: Add new feature sets
    public const string Basic = UserFeatureSets.Basic; // Free features, everyone can use
    public const string Premium = UserFeatureSets.Premium; // Premium plan features
    public const string Pro = UserFeatureSets.Pro; // Professional plan features

#if TESTINGONLY
    public const string TestingOnlyFeatures = "testingonly_organization_features";
#endif

    public static readonly IReadOnlyList<string> AssignableFeatureSets = new List<string>
    {
        // EXTEND: Add new features that Memberships will have, to control access to tenanted resources  
        Basic,
        Pro,
        Premium,
#if TESTINGONLY
        TestingOnlyFeatures
#endif
    };
}