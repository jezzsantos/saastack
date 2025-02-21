using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace CarsDomain;

public sealed class LicensePlate : ValueObjectBase<LicensePlate>
{
    public static Result<LicensePlate, Error> Create(string jurisdiction, string number)
    {
        var value1 = Jurisdiction.Create(jurisdiction);
        if (value1.IsFailure)
        {
            return value1.Error;
        }

        var value2 = NumberPlate.Create(number);
        if (value2.IsFailure)
        {
            return value2.Error;
        }

        return Create(value1.Value, value2.Value);
    }

    public static Result<LicensePlate, Error> Create(Jurisdiction jurisdiction, NumberPlate number)
    {
        return new LicensePlate(jurisdiction, number);
    }

    private LicensePlate(Jurisdiction jurisdiction, NumberPlate number)
    {
        Jurisdiction = jurisdiction;
        Number = number;
    }

    public Jurisdiction Jurisdiction { get; }

    public NumberPlate Number { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<LicensePlate> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new LicensePlate(Jurisdiction.Rehydrate()(parts[0]!, container),
                NumberPlate.Rehydrate()(parts[1]!, container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { Jurisdiction, Number };
    }
}