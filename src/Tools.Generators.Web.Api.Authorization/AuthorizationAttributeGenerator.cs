using System.Text;
using Domain.Interfaces.Authorization;
using Infrastructure.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Tools.Generators.Web.Api.Authorization;

/// <summary>
///     A source generator for converting <see cref="PlatformRoles" /> and <see cref="PlatformFeatures" /> to various
///     enumerations and mapping functions
/// </summary>
[Generator]
public class AuthorizationAttributeGenerator : ISourceGenerator
{
    private const string Filename = "AuthorizeAttribute.g.cs";

    public void Initialize(GeneratorInitializationContext context)
    {
        // No initialization
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var assemblyNamespace = context.Compilation.AssemblyName;
        var classUsingNamespaces = $"using {typeof(PlatformRoles).Namespace};";

        var fileSource = BuildFile(context, assemblyNamespace!, classUsingNamespaces);

        context.AddSource(Filename, SourceText.From(fileSource, Encoding.UTF8));

        return;

        static List<string> GetLevels(GeneratorExecutionContext context, Type collection, Type level)
        {
            var collectionMetadata = context.Compilation.GetTypeByMetadataName(collection.FullName!)!;
            var levelMetadata = context.Compilation.GetTypeByMetadataName(level.FullName!)!;
            return collectionMetadata.GetMembers()
                .Where(mem => mem.Kind == SymbolKind.Field && mem.IsStatic)
                .OfType<IFieldSymbol>()
                .Where(prop => SymbolEqualityComparer.Default.Equals(prop.Type, levelMetadata))
                .Select(symbol => symbol.Name)
                .OrderBy(name => name)
                .ToList();
        }

        static string BuildEnumFlags(List<LevelDescriptor> descriptors)
        {
            var builder = new StringBuilder();
            var counter = descriptors.Count();
            foreach (var descriptor in descriptors)
            {
                builder.AppendFormat("    {0} = 1 << {1}", descriptor.EnumName, descriptor.EnumShiftIndex);
                if (--counter > 0)
                {
                    builder.AppendLine(",");
                }
            }

            return builder.ToString();
        }

        static string BuildNameToLevelConversions(List<LevelDescriptor> descriptors)
        {
            var builder = new StringBuilder();
            foreach (var descriptor in descriptors)
            {
                builder.AppendFormat(@"            if (name == {0}.{1})",
                    descriptor.QualifiedName, nameof(FeatureLevel.Name));
                builder.AppendLine();
                builder.AppendLine(@"            {");
                builder.AppendFormat(@"                return {0};", descriptor.QualifiedName);
                builder.AppendLine();
                builder.AppendLine(@"            }");
            }

            return builder.ToString();
        }

        static string BuildFlagToLevelConversion(List<LevelDescriptor> descriptors)
        {
            var builder = new StringBuilder();
            var counter = descriptors.Count();
            foreach (var descriptor in descriptors)
            {
                builder.AppendFormat(@"            {0} => {1}", descriptor.FullyQualifiedName,
                    descriptor.QualifiedName);
                if (--counter > 0)
                {
                    builder.AppendLine(",");
                }
                else
                {
                    builder.Append(",");
                }
            }

            return builder.ToString();
        }

        static string BuildFile(GeneratorExecutionContext context, string assemblyNamespace,
            string allUsingNamespaces)
        {
            const string platformPrefix = AuthenticationConstants.Claims.PlatformPrefix;
            const string tenantPrefix = AuthenticationConstants.Claims.TenantPrefix;
            const string featureLevelName = nameof(FeatureLevel);
            const string roleLevelName = nameof(RoleLevel);
            const string rolesEnumName = "Roles";
            const string featuresEnumName = "Features";

            var indexOfRole = 0;
            var platformRoles = GetLevels(context, typeof(PlatformRoles), typeof(RoleLevel))
                .Select(rol => new LevelDescriptor
                {
                    EnumName = $"{AuthenticationConstants.Claims.PlatformPrefix}_{rol}",
                    EnumShiftIndex = indexOfRole++,
                    QualifiedName = $"{nameof(PlatformRoles)}.{rol}",
                    FullyQualifiedName = $"{rolesEnumName}.{AuthenticationConstants.Claims.PlatformPrefix}_{rol}"
                })
                .ToList();
            var tenantRoles = GetLevels(context, typeof(TenantRoles), typeof(RoleLevel))
                .Select(rol => new LevelDescriptor
                {
                    EnumName = $"{AuthenticationConstants.Claims.TenantPrefix}_{rol}",
                    EnumShiftIndex = indexOfRole++,
                    QualifiedName = $"{nameof(TenantRoles)}.{rol}",
                    FullyQualifiedName = $"{rolesEnumName}.{AuthenticationConstants.Claims.TenantPrefix}_{rol}"
                })
                .ToList();
            var allRoles = platformRoles
                .Concat(tenantRoles)
                .ToList();

            var indexOfFeature = 0;
            var platformFeatures = GetLevels(context, typeof(PlatformFeatures), typeof(FeatureLevel))
                .Select(feat => new LevelDescriptor
                {
                    EnumName = $"{AuthenticationConstants.Claims.PlatformPrefix}_{feat}",
                    EnumShiftIndex = indexOfFeature++,
                    QualifiedName = $"{nameof(PlatformFeatures)}.{feat}",
                    FullyQualifiedName = $"{featuresEnumName}.{AuthenticationConstants.Claims.PlatformPrefix}_{feat}"
                })
                .ToList();
            var tenantFeatures = GetLevels(context, typeof(TenantFeatures), typeof(FeatureLevel))
                .Select(feat => new LevelDescriptor
                {
                    EnumName = $"{AuthenticationConstants.Claims.TenantPrefix}_{feat}",
                    EnumShiftIndex = indexOfFeature++,
                    QualifiedName = $"{nameof(TenantFeatures)}.{feat}",
                    FullyQualifiedName = $"{featuresEnumName}.{AuthenticationConstants.Claims.TenantPrefix}_{feat}"
                })
                .ToList();
            var allFeatures = platformFeatures
                .Concat(tenantFeatures)
                .ToList();

            var roleFlags = BuildEnumFlags(allRoles);
            var featureFlags = BuildEnumFlags(allFeatures);

            var platformToFeatureByName = BuildNameToLevelConversions(platformFeatures);
            var tenantToFeatureByName = BuildNameToLevelConversions(tenantFeatures);
            var platformToRoleByName = BuildNameToLevelConversions(platformRoles);
            var tenantToRoleByName = BuildNameToLevelConversions(tenantRoles);

            var toFeatureLevel = BuildFlagToLevelConversion(allFeatures);
            var toRoleLevel = BuildFlagToLevelConversion(allRoles);

            return $@"// <auto-generated/>
{allUsingNamespaces}

namespace {assemblyNamespace};

/// <summary>
///     Provides scopes for both {platformPrefix} and {tenantPrefix}
/// </summary>
public enum RoleAndFeatureScope
{{
    {platformPrefix} = 0,
    {tenantPrefix} = 1
}}

/// <summary>
///     Provides all roles for both {platformPrefix} and {tenantPrefix}
/// </summary>
[Flags]
public enum {rolesEnumName}
{{
{roleFlags}
}}

/// <summary>
///     Provides all features for both {platformPrefix} and {tenantPrefix}
/// </summary>
[Flags]
public enum {featuresEnumName}
{{
{featureFlags}
}}

/// <summary>
///     Provides conversions for both {platformPrefix} and {tenantPrefix}
/// </summary>
internal static class AuthorizationAttributeExtensions
{{
    /// <summary>
    ///     Converts the <see cref=""name"" /> in the specified <see cref=""scope"" /> to the appropriate
    ///     <see cref=""{featureLevelName}"" />
    /// </summary>
    public static {featureLevelName} ToFeatureByName(this string name, RoleAndFeatureScope scope)
    {{
        if (scope == RoleAndFeatureScope.{platformPrefix})
        {{
{platformToFeatureByName}
            throw new ArgumentOutOfRangeException(nameof(name), name, null);
        }}

        if (scope == RoleAndFeatureScope.{tenantPrefix})
        {{
{tenantToFeatureByName}
            throw new ArgumentOutOfRangeException(nameof(name), name, null);
        }}

        throw new ArgumentOutOfRangeException(nameof(name), name, null);
    }}

    /// <summary>
    ///     Converts an individual <see cref=""feature"" /> flag to its respective <see cref=""{featureLevelName}"" />
    /// </summary>
    public static {featureLevelName} ToFeatureLevel(this {featuresEnumName} feature)
    {{
        return feature switch
        {{
{toFeatureLevel}
            _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
        }};
    }}

    /// <summary>
    ///     Converts the <see cref=""name"" /> in the specified <see cref=""scope"" /> to the appropriate <see cref=""{roleLevelName}"" />
    /// </summary>
    public static {roleLevelName} ToRoleByName(this string name, RoleAndFeatureScope scope)
    {{
        if (scope == RoleAndFeatureScope.{platformPrefix})
        {{
{platformToRoleByName}
            throw new ArgumentOutOfRangeException(nameof(name), name, null);
        }}

        if (scope == RoleAndFeatureScope.{tenantPrefix})
        {{
{tenantToRoleByName}
            throw new ArgumentOutOfRangeException(nameof(name), name, null);
        }}

        throw new ArgumentOutOfRangeException(nameof(name), name, null);
    }}

    /// <summary>
    ///     Converts an individual <see cref=""role"" /> flag to its respective <see cref=""{roleLevelName}"" />
    /// </summary>
    public static {roleLevelName} ToRoleLevel(this {rolesEnumName} role)
    {{
        return role switch
        {{
{toRoleLevel}
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        }};
    }}
}}
";
        }
    }
}

public class LevelDescriptor
{
    public string? EnumName { get; set; }

    public int EnumShiftIndex { get; set; }

    public string? FullyQualifiedName { get; set; }

    public string? QualifiedName { get; set; }
}