using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Validations;

namespace Domain.Shared;

public sealed class Role : SingleValueObjectBase<Role, string>
{
    public static Result<Role, Error> Create(string identifier)
    {
        if (identifier.IsNotValuedParameter(nameof(identifier), out var error1))
        {
            return error1;
        }

        if (identifier.IsInvalidParameter(CommonValidations.RoleLevel, nameof(identifier),
                Resources.Roles_InvalidRole, out var error2))
        {
            return error2;
        }

        return new Role(identifier);
    }

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

    public static ValueObjectFactory<Role> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Role(parts[0]!);
        };
    }
}