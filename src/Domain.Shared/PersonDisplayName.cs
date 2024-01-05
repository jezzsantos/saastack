using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace Domain.Shared;

public sealed class PersonDisplayName : SingleValueObjectBase<PersonDisplayName, string>
{
    public static Result<PersonDisplayName, Error> Create(string displayName)
    {
        if (displayName.IsNotValuedParameter(nameof(displayName), out var error))
        {
            return error;
        }

        return new PersonDisplayName(displayName);
    }

    private PersonDisplayName(string displayName) : base(displayName)
    {
    }

    public string Text => Value;

    public static ValueObjectFactory<PersonDisplayName> Rehydrate()
    {
        return (property, _) => new PersonDisplayName(property);
    }
}