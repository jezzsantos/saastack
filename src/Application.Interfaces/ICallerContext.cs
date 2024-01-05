using Common;
using Domain.Interfaces.Authorization;

namespace Application.Interfaces;

/// <summary>
///     The context of the currently identified caller
/// </summary>
public interface ICallerContext
{
    /// <summary>
    ///     Defines the scheme of the authorization
    /// </summary>
    public enum AuthorizationMethod
    {
        Token = 0,
        APIKey = 1,
        HMAC = 2
    }

    /// <summary>
    ///     The authorization token of the call. Passed to downstream clients
    /// </summary>
    Optional<CallerAuthorization> Authorization { get; }

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
    ///     Defines the authorization details of the caller
    /// </summary>
    public class CallerAuthorization
    {
        public CallerAuthorization(AuthorizationMethod method, string value)
        {
            Method = method;
            Value = value;
        }

        public AuthorizationMethod Method { get; }

        public string Value { get; }
    }

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

        public CallerRoles(string[]? user, string[]? member)
        {
            User = user ?? Array.Empty<string>();
            Organization = member ?? Array.Empty<string>();
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

        public CallerFeatureLevels(FeatureLevel[]? platform, FeatureLevel[]? member)
        {
            Platform = platform ?? Array.Empty<FeatureLevel>();
            Organization = member ?? Array.Empty<FeatureLevel>();
            All = Platform.Concat(Organization)
                .ToArray();
        }

        public FeatureLevel[] All { get; }

        public FeatureLevel[] Organization { get; }

        public FeatureLevel[] Platform { get; }
    }
}