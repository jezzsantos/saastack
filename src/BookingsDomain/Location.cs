using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.ValueObjects;

namespace BookingsDomain;

public sealed class Location : SingleValueObjectBase<Location, string>
{
    public static Result<Location, Error> Create(string name)
    {
        if (name.IsNotValuedParameter(nameof(name), out var error))
        {
            return error;
        }

        return new Location(name);
    }

    private Location(string name) : base(name)
    {
    }

    public static ValueObjectFactory<Location> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Location(parts[0]);
        };
    }

    public string Name => Value;
}