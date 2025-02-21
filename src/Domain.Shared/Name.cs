using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared;

public sealed class Name : SingleValueObjectBase<Name, string>
{
    public static Result<Name, Error> Create(string name)
    {
        if (name.IsNotValuedParameter(nameof(name), out var error))
        {
            return error;
        }

        return new Name(name);
    }

    private Name(string name) : base(name)
    {
    }

    public string Text => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Name> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Name(parts[0]!);
        };
    }
}