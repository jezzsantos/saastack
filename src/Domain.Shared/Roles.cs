using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Extensions;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared;

/// <summary>
///     Defines a collection of normalized <see cref="Role" />.
///     Since we only store the Name of the <see cref="RoleLevel" /> we need to maintain a normalized collection
///     of <see cref="Role" />.
/// </summary>
public sealed class Roles : SingleValueObjectBase<Roles, List<Role>>
{
    public static readonly Roles Empty = new();

    public static Result<Roles, Error> Create(string role)
    {
        return Create(role.ToRoleLevel());
    }

    public static Result<Roles, Error> Create(RoleLevel role)
    {
        return Create([role]);
    }

    public static Result<Roles, Error> Create(params string[] roles)
    {
        return Create(roles.Select(role => role.ToRoleLevel()).ToArray());
    }

    public static Result<Roles, Error> Create(params RoleLevel[] roles)
    {
        var normalized = roles.Normalize();
        var list = new List<Role>();
        foreach (var role in normalized)
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

    private Roles() : base([])
    {
    }

    private Roles(IEnumerable<Role> roles) : base(roles.ToList())
    {
    }

    public List<Role> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Roles> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            var roles = items.Select(item => Role.Rehydrate()(item, container));
            return new Roles(roles);
        };
    }

    public Result<Roles, Error> Add(string role)
    {
        var rol = Role.Create(role.ToRoleLevel());
        if (rol.IsFailure)
        {
            return rol.Error;
        }

        return Add(rol.Value);
    }

    public Result<Roles, Error> Add(RoleLevel role)
    {
        var rol = Role.Create(role);
        if (rol.IsFailure)
        {
            return rol.Error;
        }

        return Add(rol.Value);
    }

#pragma warning disable CA1822
    public Roles Clear()
#pragma warning restore CA1822
    {
        return Empty;
    }

    [SkipImmutabilityCheck]
    public List<string> Denormalize()
    {
        return Items
            .Select(rol => rol.AsLevel())
            .ToArray()
            .Denormalize()
            .ToList();
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
    public bool HasRole(Role role)
    {
        var denormalized = Denormalize();
        return denormalized.ContainsIgnoreCase(role.Identifier);
    }

    [SkipImmutabilityCheck]
    public bool HasRole(RoleLevel role)
    {
        var rol = Role.Create(role);
        if (rol.IsFailure)
        {
            return false;
        }

        return HasRole(rol.Value);
    }

    public Roles Remove(string role)
    {
        return Remove(role.ToRoleLevel());
    }

    public Roles Remove(RoleLevel role)
    {
        var rol = Role.Create(role);
        if (rol.IsFailure)
        {
            return this;
        }

        return Remove(rol.Value);
    }

    private Roles Add(Role role)
    {
        if (HasRole(role))
        {
            return new Roles(Value);
        }

        var roles = Value
            .Select(val => val.AsLevel())
            .ToArray()
            .Merge(role.AsLevel())
            .Select(level => Role.Create(level).Value);

        return new Roles(roles);
    }

    private Roles Remove(Role role)
    {
        if (!HasRole(role))
        {
            return new Roles(Value);
        }

        var roles = Value
            .Select(rol => rol.AsLevel())
            .ToArray()
            .UnMerge(role.AsLevel())
            .Select(level => Role.Create(level).Value);

        return new Roles(roles);
    }
}