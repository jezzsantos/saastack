using System.Text;
using Application.Interfaces;
using Common.Extensions;
using Domain.Interfaces.Authorization;
#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
using System.Runtime.Serialization;
#endif

namespace Infrastructure.Web.Api.Interfaces;

/// <summary>
///     Provides a declarative way to define authorization on a service operation
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class AuthorizeAttribute : Attribute
{
    private const char DoubleQuoteReplacementChar = '|';
    private const string PolicyHeader = "POLICY:";

    public AuthorizeAttribute(Roles beOneOf)
    {
        Roles = ProcessRoles(beOneOf);
        Features = ProcessFeatures();
    }

    public AuthorizeAttribute(Features haveAtLeast)
    {
        Roles = ProcessRoles();
        Features = ProcessFeatures(haveAtLeast);
    }

    public AuthorizeAttribute(Roles beOneOf, Features haveAtLeast)
    {
        Roles = ProcessRoles(beOneOf);
        Features = ProcessFeatures(haveAtLeast);
    }

    public AuthorizeAttribute(Features haveAtLeast, Roles beOneOf)
    {
        Roles = ProcessRoles(beOneOf);
        Features = ProcessFeatures(haveAtLeast);
    }

    public ICallerContext.CallerFeatures Features { get; }

    public ICallerContext.CallerRoles Roles { get; }

    /// <summary>
    ///     Converts the <see cref="rolesAndFeaturesSets" /> into their respective <see cref="PlatformRoles" />,
    ///     <see cref="PlatformFeatures" />, normalizes them, and then constructs a unique policy name for each of them.
    ///     Note: each role or feature should come in this form:
    ///     e.g. "Role.Platform_Role1" or "Feature.Organization_Feature1"
    /// </summary>
    public static string CreatePolicyName(IReadOnlyList<IReadOnlyList<string>> rolesAndFeaturesSets)
    {
        if (rolesAndFeaturesSets.HasNone())
        {
            return AuthenticationConstants.Authorization.RolesAndFeaturesPolicyNameForNone;
        }

        var setPolicyNames = new List<string>();
        foreach (var set in rolesAndFeaturesSets)
        {
            var (roles, features) = ParseRoleOrFeatureName(set);
            var setPolicyName = CreatePolicyNameForSet(roles, features);
#if NETSTANDARD2_0
            if ((setPolicyName ?? string.Empty).HasValue())
            {
                setPolicyNames.Add(setPolicyName!);
            }
#else
            if (setPolicyName.HasValue())
            {
                setPolicyNames.Add(setPolicyName);
            }
#endif
        }

        return CreatePoliciesName(setPolicyNames);
    }

    internal static string FormatFeatureName(Features flag)
    {
        return $"{nameof(Interfaces.Features)}.{flag}";
    }

    internal static string FormatRoleName(Roles flag)
    {
        return $"{nameof(Roles)}.{flag}";
    }

    /// <summary>
    ///     Parses the specified <see cref="policyName" /> and extracts the normalized
    ///     <see cref="ICallerContext.CallerRoles" /> and the normalized
    ///     <see cref="ICallerContext.CallerFeatures" /> for each policy
    ///     Note: a policy may look like this:
    ///     "POLICY:{|Features|:{|Platform|:[|basic_features|]},|Roles|:{|Platform|:[|{platform_standard}|]}}"
    /// </summary>
    public static IReadOnlyList<(ICallerContext.CallerRoles Roles, ICallerContext.CallerFeatures Features)>
        ParsePolicyName(string policyName)
    {
        if (policyName.HasNoValue())
        {
            return new List<(ICallerContext.CallerRoles Roles, ICallerContext.CallerFeatures Features)>();
        }

        if (policyName == AuthenticationConstants.Authorization.RolesAndFeaturesPolicyNameForNone)
        {
            return new List<(ICallerContext.CallerRoles Roles, ICallerContext.CallerFeatures Features)>();
        }

        var policies = policyName
            .Split([PolicyHeader], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim().Replace(DoubleQuoteReplacementChar, '"'))
            .Select(item => item.FromJson<PolicyName>())
            .Where(policy => policy is not null)
            .Select(policy =>
            {
                var roles = policy!.Roles is null
                    ? new ICallerContext.CallerRoles()
                    : new ICallerContext.CallerRoles(policy.Roles.Platform is not null
                        ? policy.Roles.Platform.Select(rol => rol.ToRoleByName(RoleAndFeatureScope.Platform)).ToArray()
                        : [], policy.Roles.Tenant is not null
                        ? policy.Roles.Tenant.Select(rol => rol.ToRoleByName(RoleAndFeatureScope.Tenant)).ToArray()
                        : []);
                var features = policy.Features is null
                    ? new ICallerContext.CallerFeatures()
                    : new ICallerContext.CallerFeatures(policy.Features.Platform is not null
                            ? policy.Features.Platform
                                .Select(feat => feat.ToFeatureByName(RoleAndFeatureScope.Platform))
                                .ToArray()
                            : [],
                        policy.Features.Tenant is not null
                            ? policy.Features.Tenant.Select(feat => feat.ToFeatureByName(RoleAndFeatureScope.Tenant))
                                .ToArray()
                            : []);
                return (roles, features);
            })
            .ToList();

        return policies;
    }

    private static string? CreatePolicyNameForSet(ICallerContext.CallerRoles roles,
        ICallerContext.CallerFeatures features)
    {
        if (roles.All.HasNone() && features.All.HasNone())
        {
            return null;
        }

        return (new PolicyName
            {
                Roles = new PolicyNameStage
                {
                    Platform = roles.Platform.HasAny()
                        ? roles.Platform.Select(rol => rol.Name).ToList()
                        : null,
                    Tenant = roles.Tenant.HasAny()
                        ? roles.Tenant.Select(rol => rol.Name).ToList()
                        : null
                },
                Features = new PolicyNameStage
                {
                    Platform = features.Platform.HasAny()
                        ? features.Platform.Select(feat => feat.Name).ToList()
                        : null,
                    Tenant = features.Tenant.HasAny()
                        ? features.Tenant.Select(feat => feat.Name).ToList()
                        : null
                }
            }.ToJson(false) ?? string.Empty)
            .Replace('"', DoubleQuoteReplacementChar);
    }

    private static (ICallerContext.CallerRoles Roles, ICallerContext.CallerFeatures Features) ParseRoleOrFeatureName(
        IReadOnlyList<string> rolesOrFeatures)
    {
        var platformRoles = new List<RoleLevel>();
        var tenantRoles = new List<RoleLevel>();
        var platformFeatures = new List<FeatureLevel>();
        var tenantFeatures = new List<FeatureLevel>();
        foreach (var roleOrFeature in rolesOrFeatures)
        {
            var parts = roleOrFeature.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var prefix = parts[0];
            var value = parts[1];
            switch (prefix)
            {
                case nameof(Roles):
                {
                    if (!Enum.TryParse<Roles>(value, out var role))
                    {
                        continue;
                    }

                    if (value.StartsWith(AuthenticationConstants.Claims.PlatformPrefix))
                    {
                        platformRoles.Add(role.ToRoleLevel());
                    }

                    if (value.StartsWith(AuthenticationConstants.Claims.TenantPrefix))
                    {
                        tenantRoles.Add(role.ToRoleLevel());
                    }

                    break;
                }

                case nameof(Interfaces.Features):
                {
                    if (!Enum.TryParse<Features>(value, out var role))
                    {
                        continue;
                    }

                    if (value.StartsWith(AuthenticationConstants.Claims.PlatformPrefix))
                    {
                        platformFeatures.Add(role.ToFeatureLevel());
                    }

                    if (value.StartsWith(AuthenticationConstants.Claims.TenantPrefix))
                    {
                        tenantFeatures.Add(role.ToFeatureLevel());
                    }

                    break;
                }

                default:
                    continue;
            }
        }

        if (platformRoles.HasNone() && tenantRoles.HasNone())
        {
            platformRoles.Add(PlatformRoles.Standard);
        }

        if (platformFeatures.HasNone() && tenantFeatures.HasNone())
        {
            platformFeatures.Add(PlatformFeatures.Basic);
        }

        return (new ICallerContext.CallerRoles(platformRoles.ToArray(), tenantRoles.ToArray()),
            new ICallerContext.CallerFeatures(platformFeatures.ToArray(), tenantFeatures.ToArray()));
    }

    private static string CreatePoliciesName(List<string> policies)
    {
        if (policies.HasNone())
        {
            return AuthenticationConstants.Authorization.RolesAndFeaturesPolicyNameForNone;
        }

        var result = new StringBuilder();
        foreach (var policy in policies)
        {
            if (policy.HasNoValue())
            {
                continue;
            }

            result.Append($"{PolicyHeader}{policy}");
        }

        return result.ToString();
    }

    private static ICallerContext.CallerFeatures ProcessFeatures(Features? features = null)
    {
        var allFeatures = features ?? 0;
        var platform = ToPlatform(allFeatures);
        var tenant = ToTenant(allFeatures);
        if (platform.HasNone() && tenant.HasNone())
        {
            platform = [PlatformFeatures.Basic];
        }

        return new ICallerContext.CallerFeatures(platform, tenant);
    }

    private static ICallerContext.CallerRoles ProcessRoles(Roles? roles = null)
    {
        var allRoles = roles ?? 0;
        var platform = ToPlatform(allRoles);
        var tenant = ToTenant(allRoles);
        if (platform.HasNone() && tenant.HasNone())
        {
            platform = [PlatformRoles.Standard];
        }

        return new ICallerContext.CallerRoles(platform, tenant);
    }

    private static IEnumerable<TFlagEnum> ToEnumValues<TFlagEnum>(TFlagEnum flags, string prefix)
        where TFlagEnum : struct
    {
        var values = flags.ToString() ?? string.Empty;
        if (values.HasNoValue())
        {
            return [];
        }

        return values
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.ToString().StartsWith(prefix))
            .Select(item => (TFlagEnum)Enum.Parse(typeof(TFlagEnum), item, true))
            .ToArray();
    }

    private static RoleLevel[] ToPlatform(Roles roles)
    {
        return ToEnumValues(roles, AuthenticationConstants.Claims.PlatformPrefix)
            .Select(value => value.ToRoleLevel())
            .ToArray();
    }

    private static RoleLevel[] ToTenant(Roles roles)
    {
        return ToEnumValues(roles, AuthenticationConstants.Claims.TenantPrefix)
            .Select(value => value.ToRoleLevel())
            .ToArray();
    }

    private static FeatureLevel[] ToPlatform(Features features)
    {
        return ToEnumValues(features, AuthenticationConstants.Claims.PlatformPrefix)
            .Select(value => value.ToFeatureLevel())
            .ToArray();
    }

    private static FeatureLevel[] ToTenant(Features features)
    {
        return ToEnumValues(features, AuthenticationConstants.Claims.TenantPrefix)
            .Select(value => value.ToFeatureLevel())
            .ToArray();
    }

    /// <summary>
    ///     Provides a serializable class for storing policy names
    /// </summary>
#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
    [DataContract]
#endif
    public class PolicyName
    {
#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
        [DataMember(Order = 1, EmitDefaultValue = false)]
#endif
        public PolicyNameStage? Features { get; set; }

#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
        [DataMember(Order = 2, EmitDefaultValue = false)]
#endif
        public PolicyNameStage? Roles { get; set; }
    }

    /// <summary>
    ///     Provides a serializable class for storing policy stages
    /// </summary>
#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
    [DataContract]
#endif
    public class PolicyNameStage
    {
#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
        [DataMember(Order = 1, EmitDefaultValue = false)]
#endif
        public List<string>? Platform { get; set; }
#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
        [DataMember(Order = 1, EmitDefaultValue = false)]
#endif
        public List<string>? Tenant { get; set; }
    }
}