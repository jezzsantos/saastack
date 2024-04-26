using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.ValueObjects;

namespace Domain.Shared;

public sealed class Roles : SingleValueObjectBase<Roles, List<Role>>
{
    public static readonly Roles Empty = new();

    public static Roles Create()
    {
        return new Roles();
    }

    public static Result<Roles, Error> Create(string role)
    {
        var rol = Role.Create(role);
        if (rol.IsFailure)
        {
            return rol.Error;
        }

        return new Roles(rol.Value);
    }

    public static Result<Roles, Error> Create(RoleLevel role)
    {
        var allLevels = role.AllDescendantNames().ToArray();
        return Create(allLevels);
    }

    public static Result<Roles, Error> Create(params string[] roles)
    {
        var list = new List<Role>();
        foreach (var role in roles)
        {
            var rol = Role.Create(role);
            if (rol.IsFailure)
            {
                return rol.Error;
            }

            list.Add(rol.Value);
        }

        return new Roles(list);
    }

    public static Result<Roles, Error> Create(params RoleLevel[] roles)
    {
        var list = new List<Role>();
        foreach (var role in roles)
        {
            var rol = Role.Create(role);
            if (rol.IsFailure)
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

    private Roles(Role roles) : base(new List<Role> { roles })

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
        if (rol.IsFailure)
        {
            return rol.Error;
        }

        return Add(rol.Value);
    }

    public Result<Roles, Error> Add(RoleLevel role)
    {
        var roles = new Roles(Value.ToArray());
        var allLevels = role.AllDescendantNames().ToArray();
        foreach (var level in allLevels)
        {
            var added = roles.Add(level);
            if (added.IsFailure)
            {
                return added.Error;
            }

            roles = added.Value;
        }

        return roles;
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
        if (rol.IsFailure)
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

    [SkipImmutabilityCheck]
    public bool HasRole(RoleLevel role)
    {
        return HasRole(role.Name);
    }

    public Roles Remove(string role)
    {
        var rol = Role.Create(role);
        if (rol.IsFailure)
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