using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.ValueObjects;

namespace CarsDomain;

public sealed class VehicleManagers : ValueObjectBase<VehicleManagers>
{
    private readonly List<Identifier> _managers;

    public static Result<VehicleManagers, Error> Create(string managerId)
    {
        if (managerId.IsNotValuedParameter(managerId, nameof(managerId), out var error))
        {
            return error;
        }

        return new VehicleManagers(new List<Identifier> { managerId.ToId() });
    }

    public static VehicleManagers Create()
    {
        return new VehicleManagers(new List<Identifier>());
    }

    private VehicleManagers() : this(new List<Identifier>())
    {
    }

    private VehicleManagers(List<Identifier> managerIds)
    {
        _managers = managerIds;
    }

    public IReadOnlyList<Identifier> Managers => _managers;

    public override string Dehydrate()
    {
        return _managers
            .Select(man => man)
            .Join(";");
    }

    public static ValueObjectFactory<VehicleManagers> Rehydrate()
    {
        return (_, _) => new VehicleManagers();
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new[] { Managers };
    }

    public VehicleManagers Append(Identifier id)
    {
        var ids = new List<Identifier>(_managers);
        if (!ids.Contains(id))
        {
            ids.Add(id);
        }

        return new VehicleManagers(ids);
    }
}