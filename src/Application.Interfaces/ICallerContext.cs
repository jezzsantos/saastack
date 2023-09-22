namespace Application.Interfaces;

/// <summary>
///     The context of the current caller
/// </summary>
public interface ICallerContext
{
    string CallId { get; }

    string CallerId { get; }

    string? TenantId { get; }

    CallerRoles Roles { get; }

    CallerFeatureSets FeatureSets { get; }

    string? Authorization { get; }

    public bool IsAuthenticated { get; }

    public bool IsServiceAccount { get; }


    /// <summary>
    ///     Defines the roles of the current caller
    /// </summary>
    public class CallerRoles
    {
        public CallerRoles()
        {
            All = Array.Empty<string>();
            User = Array.Empty<string>();
            Organisation = Array.Empty<string>();
        }

        public CallerRoles(string[]? user, string[]? organisation)
        {
            User = user ?? Array.Empty<string>();
            Organisation = organisation ?? Array.Empty<string>();
            All = User.Concat(Organisation).ToArray();
        }

        public string[] All { get; }

        public string[] User { get; }

        public string[] Organisation { get; }
    }

    /// <summary>
    ///     Defines the sets of features allowed for the current caller
    /// </summary>
    public class CallerFeatureSets
    {
        public CallerFeatureSets()
        {
            All = Array.Empty<string>();
            User = Array.Empty<string>();
            Organisation = Array.Empty<string>();
        }

        public CallerFeatureSets(string[]? user, string[]? organisation)
        {
            User = user ?? Array.Empty<string>();
            Organisation = organisation ?? Array.Empty<string>();
            All = User.Concat(Organisation).ToArray();
        }

        public string[] All { get; }

        public string[] User { get; }

        public string[] Organisation { get; }
    }
}