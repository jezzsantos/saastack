using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared;

public sealed class PersonName : ValueObjectBase<PersonName>
{
    public static Result<PersonName, Error> Create(string firstName, Optional<string> lastName)
    {
        var name1 = Name.Create(firstName);
        if (name1.IsFailure)
        {
            return name1.Error;
        }

        if (lastName.HasValue)
        {
            var name2 = Name.Create(lastName);
            if (name2.IsFailure)
            {
                return name2.Error;
            }

            return new PersonName(name1.Value, name2.Value);
        }

        return new PersonName(name1.Value, Optional<Name>.None);
    }

    public static Result<PersonName, Error> Create(Name firstName, Optional<Name> lastName)
    {
        return new PersonName(firstName, lastName);
    }

    private PersonName(Name firstName, Optional<Name> lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public Name FirstName { get; }

    public Name FullName => LastName.HasValue
        ? Name.Create($"{FirstName} {LastName}").Value
        : Name.Create($"{FirstName}").Value;

    public Optional<Name> LastName { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<PersonName> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new PersonName(
                Name.Rehydrate()(parts[0]!, container),
                parts[1].FromValueOrNone(val => Name.Rehydrate()(val, container)));
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { FirstName, LastName };
    }
}