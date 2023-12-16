using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace Domain.Shared;

public sealed class Name : SingleValueObjectBase<Name, string>
{
    public static Result<Name, Error> Create(string name)
    {
        var value = new Name(name);
        if (name.IsNotValuedParameter(nameof(name), out var error))
        {
            return error;
        }

        return value;
    }

    private Name(string name) : base(name)
    {
    }

    public string Text => Value;

    public static ValueObjectFactory<Name> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Name(parts[0]!);
        };
    }
}