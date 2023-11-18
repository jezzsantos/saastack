using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace CarsDomain;

public sealed class VehicleOwner : SingleValueObjectBase<VehicleOwner, string>
{
    public static Result<VehicleOwner, Error> Create(string ownerId)
    {
        if (ownerId.IsNotValuedParameter(ownerId, nameof(ownerId), out var error))
        {
            return error;
        }

        return new VehicleOwner(ownerId);
    }

    private VehicleOwner(string ownerId) : base(ownerId)
    {
    }

    public string OwnerId => Value;

    public static ValueObjectFactory<VehicleOwner> Rehydrate()
    {
        return (property, _) => new VehicleOwner(property);
    }
}