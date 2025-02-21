using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace CarsDomain;

public sealed class NumberPlate : SingleValueObjectBase<NumberPlate, string>
{
    public static Result<NumberPlate, Error> Create(string number)
    {
        if (number.IsNotValuedParameter(number, nameof(number), out var error1))
        {
            return error1;
        }

        if (number.IsInvalidParameter(Validations.Car.NumberPlate, nameof(number),
                Resources.NumberPlate_InvalidNumberPlate, out var error2))
        {
            return error2;
        }

        return new NumberPlate(number);
    }

    private NumberPlate(string number) : base(number)
    {
    }

    public string Registration => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<NumberPlate> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new NumberPlate(parts[0]!);
        };
    }
}