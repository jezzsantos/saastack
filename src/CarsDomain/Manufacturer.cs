using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared;

namespace CarsDomain;

public sealed class Manufacturer : ValueObjectBase<Manufacturer>
{
    public static readonly IReadOnlyList<string> AllowedMakes = new List<string> { "Honda", "Toyota" };
    public static readonly IReadOnlyList<string> AllowedModels = new List<string> { "Civic", "Surf" };

    public static Result<Manufacturer, Error> Create(int year, string make, string model)
    {
        var newYear = Year.Create(year);
        if (newYear.IsFailure)
        {
            return newYear.Error;
        }

        var newMake = Name.Create(make);
        if (newMake.IsFailure)
        {
            return newMake.Error;
        }

        var newModel = Name.Create(model);
        if (newModel.IsFailure)
        {
            return newModel.Error;
        }

        return Create(newYear.Value, newMake.Value, newModel.Value);
    }

    public static Result<Manufacturer, Error> Create(Year year, Name make, Name model)
    {
        if (make.IsInvalidParameter(m => AllowedMakes.Contains(m), nameof(make), Resources.Manufacturer_UnknownMake,
                out var error1))
        {
            return error1;
        }

        if (model.IsInvalidParameter(m => AllowedModels.Contains(m), nameof(model), Resources.Manufacturer_UnknownModel,
                out var error2))
        {
            return error2;
        }

        return new Manufacturer(year, make, model);
    }

    private Manufacturer(Year year, Name make, Name model)
    {
        Year = year;
        Make = make;
        Model = model;
    }

    public Name Make { get; }

    public Name Model { get; }

    public Year Year { get; }

    public static ValueObjectFactory<Manufacturer> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new Manufacturer(Year.Rehydrate()(parts[0]!, container), Name.Rehydrate()(parts[1]!, container),
                Name.Rehydrate()(parts[2]!, container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { Year, Make, Model };
    }
}