using Domain.Interfaces.Authorization;
using Domain.Interfaces.Extensions;

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
            All = [];
            Platform = [];
            Tenant = [];
        }

        public CallerRoles(RoleLevel[]? platform, RoleLevel[]? tenant)
        {
            Platform = platform.Normalize();
            Tenant = tenant.Normalize();
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
            All = [];
            Platform = [];
            Tenant = [];
        }

        public CallerFeatures(FeatureLevel[]? platform, FeatureLevel[]? tenant)
        {
            Platform = platform.Normalize();
            Tenant = tenant.Normalize();
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