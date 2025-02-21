using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Validations;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared;

/// <summary>
///     Defines the name of a role. We store the name of the <see cref="RoleLevel" /> only for serialization purposes
/// </summary>
public sealed class Role : SingleValueObjectBase<Role, string>
{
    public static Result<Role, Error> Create(RoleLevel level)
    {
        if (level.Name.IsInvalidParameter(CommonValidations.RoleLevel, nameof(level.Name),
                Resources.Roles_InvalidRole, out var error))
        {
            return error;
        }

        return new Role(level.Name);
    }

    private Role(string identifier) : base(identifier.ToLowerInvariant())
    {
    }

    public string Identifier => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Role> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Role(parts[0]!);
        };
    }

    [SkipImmutabilityCheck]
    public RoleLevel AsLevel()
    {
        return Identifier.ToRoleLevel();
    }
}

public static class RoleExtensions
{
    public static RoleLevel ToRoleLevel(this string name)
    {
        var platform = PlatformRoles.FindRoleByName(name);
        if (platform.Exists())
        {
            return platform;
        }

        var tenant = TenantRoles.FindRoleByName(name);
        if (tenant.Exists())
        {
            return tenant;
        }

        return new RoleLevel(name);
    }
}