using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Validations;
using Domain.Interfaces.ValueObjects;

namespace Domain.Shared;

public class Role : SingleValueObjectBase<Role, string>
{
    public static Result<Role, Error> Create(string identifier)
    {
        if (identifier.IsNotValuedParameter(nameof(identifier), out var error1))
        {
            return error1;
        }

        if (identifier.IsInvalidParameter(CommonValidations.Role, nameof(identifier),
                Resources.Roles_InvalidRole, out var error2))
        {
            return error2;
        }

        if (identifier.IsInvalidParameter(
                rol => PlatformRoles.IsPlatformAssignableRole(rol) || MemberRoles.IsMemberAssignableRole(rol),
                nameof(identifier), Resources.Roles_InvalidRole, out var error3))
        {
            return error3;
        }

        return new Role(identifier);
    }

    private Role(string identifier) : base(identifier.ToLowerInvariant())
    {
    }

    public string Identifier => Value;

    public static ValueObjectFactory<Role> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Role(parts[0]!);
        };
    }
}

public class Roles : SingleValueObjectBase<Roles, List<Role>>
{
    public static readonly Roles Empty = new();

    public static Result<Roles, Error> Create()
    {
        return new Roles();
    }

    public static Result<Roles, Error> Create(string role)
    {
        var rol = Role.Create(role);
        if (!rol.IsSuccessful)
        {
            return rol.Error;
        }

        return new Roles(rol.Value);
    }

    public static Result<Roles, Error> Create(IEnumerable<string> roles)
    {
        var list = new List<Role>();
        foreach (var role in roles)
        {
            var rol = Role.Create(role);
            if (!rol.IsSuccessful)
            {
                return rol.Error;
            }

            list.Add(rol.Value);
        }

        return new Roles(list);
    }

    private Roles() : base(new List<Role>())
    {
    }

    private Roles(Role role) : base(new List<Role> { role })

    {
    }

    private Roles(IEnumerable<Role> roles) : base(roles.ToList())
    {
    }

    public List<Role> Items => Value;

    public static ValueObjectFactory<Roles> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            var roles = items.Select(item => Role.Rehydrate()(item!, container));
            return new Roles(roles);
        };
    }

    public Result<Roles, Error> Add(string role)
    {
        var rol = Role.Create(role);
        if (!rol.IsSuccessful)
        {
            return rol.Error;
        }

        return Add(rol.Value);
    }

    public Roles Add(Role role)
    {
        if (!Value.Contains(role))
        {
            var newValues = Value.Concat(new[] { role });
            return new Roles(newValues.ToArray());
        }

        return new Roles(Value);
    }

#pragma warning disable CA1822
    public Roles Clear()
#pragma warning restore CA1822
    {
        return Empty;
    }

    [SkipImmutabilityCheck]
    public bool HasAny()
    {
        return Value.HasAny();
    }

    [SkipImmutabilityCheck]
    public bool HasNone()
    {
        return Value.HasNone();
    }

    [SkipImmutabilityCheck]
    public bool HasRole(string role)
    {
        var rol = Role.Create(role);
        if (!rol.IsSuccessful)
        {
            return false;
        }

        return HasRole(rol.Value);
    }

    [SkipImmutabilityCheck]
    public bool HasRole(Role role)
    {
        return Value.ToList().Select(rol => rol.Identifier).ContainsIgnoreCase(role);
    }

    public Roles Remove(string role)
    {
        var rol = Role.Create(role);
        if (!rol.IsSuccessful)
        {
            return this;
        }

        return Remove(rol.Value);
    }

    public Roles Remove(Role role)
    {
        if (Value.Contains(role))
        {
            var remaining = Value
                .Where(rol => !rol.Equals(role))
                .ToList();

            return new Roles(remaining);
        }

        return new Roles(Value);
    }

    [SkipImmutabilityCheck]
    public List<string> ToList()
    {
        return Items.Select(rol => rol.Identifier).ToList();
    }
}