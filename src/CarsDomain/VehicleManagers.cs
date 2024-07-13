using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace CarsDomain;

public sealed class VehicleManagers : SingleValueObjectBase<VehicleManagers, List<Identifier>>
{
    public static readonly VehicleManagers Empty = new([]);

    public static Result<VehicleManagers, Error> Create(string managerId)
    {
        if (managerId.IsNotValuedParameter(managerId, nameof(managerId), out var error))
        {
            return error;
        }

        return new VehicleManagers([managerId.ToId()]);
    }

    private VehicleManagers(List<Identifier> managerIds) : base(managerIds)
    {
    }

    public IReadOnlyList<Identifier> Ids => Value;

    public static ValueObjectFactory<VehicleManagers> Rehydrate()
    {
        return (property, _) =>
        {
            var items = RehydrateToList(property, true, true);
            return new VehicleManagers(items.Select(item => item.ToId()).ToList());
        };
    }

    public VehicleManagers Append(Identifier id)
    {
        var ids = new List<Identifier>(Value);
        if (!ids.Contains(id))
        {
            ids.Add(id);
        }

        return new VehicleManagers(ids);
    }
}