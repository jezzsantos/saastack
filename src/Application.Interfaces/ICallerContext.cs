using Domain.Interfaces.Authorization;

namespace Application.Interfaces;

/// <summary>
///     The context of the currently identified caller
/// </summary>
public interface ICallerContext
{
    /// <summary>
    ///     The authorization token of the call. Passed to downstream clients
    /// </summary>
    string? Authorization { get; }

    /// <summary>
    ///     The ID of the identified caller
    /// </summary>
    string CallerId { get; }

    /// <summary>
    ///     The ID of the (correlated) call
    /// </summary>
    string CallId { get; }

    /// <summary>
    ///     The feature sets belonging to the caller
    /// </summary>
    CallerFeatureLevels FeatureLevels { get; }

    /// <summary>
    ///     Whether the called is authenticated or not
    /// </summary>
    public bool IsAuthenticated { get; }

    /// <summary>
    ///     Whether the called is an internal service account
    /// </summary>
    public bool IsServiceAccount { get; }

    /// <summary>
    ///     The authorization roles belonging to the caller
    /// </summary>
    CallerRoles Roles { get; }

    /// <summary>
    ///     The ID of the tenant of the caller
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    ///     Defines the authorization roles that a caller can have
    /// </summary>
    public class CallerRoles
    {
        public CallerRoles()
        {
            All = Array.Empty<string>();
            User = Array.Empty<string>();
            Organization = Array.Empty<string>();
        }

        public CallerRoles(string[]? user, string[]? organization)
        {
            User = user ?? Array.Empty<string>();
            Organization = organization ?? Array.Empty<string>();
            All = User.Concat(Organization)
                .ToArray();
        }

        public string[] All { get; }

        public string[] Organization { get; }

        public string[] User { get; }
    }

    /// <summary>
    ///     Defines the sets of features that a caller can have
    /// </summary>
    public class CallerFeatureLevels
    {
        public CallerFeatureLevels()
        {
            All = Array.Empty<FeatureLevel>();
            Platform = Array.Empty<FeatureLevel>();
            Organization = Array.Empty<FeatureLevel>();
        }

        public CallerFeatureLevels(FeatureLevel[]? platform, FeatureLevel[]? organization)
        {
            Platform = platform ?? Array.Empty<FeatureLevel>();
            Organization = organization ?? Array.Empty<FeatureLevel>();
            All = Platform.Concat(Organization)
                .ToArray();
        }

        public FeatureLevel[] All { get; }

        public FeatureLevel[] Organization { get; }

        public FeatureLevel[] Platform { get; }
    }
}