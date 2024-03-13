using Domain.Interfaces.Authorization;

namespace Application.Interfaces;

/// <inheritdoc cref="ICallerContext" />
public partial interface ICallerContext
{
    /// <summary>
    ///     Defines the authorization roles that a caller can have
    /// </summary>
    public class CallerRoles
    {
        public CallerRoles()
        {
            All = Array.Empty<RoleLevel>();
            Platform = Array.Empty<RoleLevel>();
            Tenant = Array.Empty<RoleLevel>();
        }

        public CallerRoles(RoleLevel[]? platform, RoleLevel[]? tenant)
        {
            Platform = platform ?? Array.Empty<RoleLevel>();
            Tenant = tenant ?? Array.Empty<RoleLevel>();
            All = Platform
                .Concat(Tenant)
                .Distinct()
                .ToArray();
        }

        public RoleLevel[] All { get; }

        public RoleLevel[] Platform { get; }

        public RoleLevel[] Tenant { get; }
    }

    /// <summary>
    ///     Defines the authorization features that a caller can have
    /// </summary>
    public class CallerFeatures
    {
        public CallerFeatures()
        {
            All = Array.Empty<FeatureLevel>();
            Platform = Array.Empty<FeatureLevel>();
            Tenant = Array.Empty<FeatureLevel>();
        }

        public CallerFeatures(FeatureLevel[]? platform, FeatureLevel[]? tenant)
        {
            Platform = platform ?? Array.Empty<FeatureLevel>();
            Tenant = tenant ?? Array.Empty<FeatureLevel>();
            All = Platform
                .Concat(Tenant)
                .Distinct()
                .ToArray();
        }

        public FeatureLevel[] All { get; }

        public FeatureLevel[] Platform { get; }

        public FeatureLevel[] Tenant { get; }
    }
}