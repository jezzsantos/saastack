using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.ValueObjects;

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

    public static ValueObjectFactory<VehicleOwner> Rehydrate()
    {
        return (property, _) => new VehicleOwner(property);
    }

    public string OwnerId => Value;
}