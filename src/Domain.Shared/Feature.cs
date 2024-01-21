using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Validations;

namespace Domain.Shared;

public class Feature : SingleValueObjectBase<Feature, string>
{
    public static Result<Feature, Error> Create(string identifier)
    {
        if (identifier.IsNotValuedParameter(nameof(identifier), out var error1))
        {
            return error1;
        }

        if (identifier.IsInvalidParameter(CommonValidations.FeatureLevel, nameof(identifier),
                Resources.Features_InvalidFeature, out var error2))
        {
            return error2;
        }

        return new Feature(identifier);
    }

    public static Result<Feature, Error> Create(FeatureLevel level)
    {
        if (level.Name.IsInvalidParameter(CommonValidations.FeatureLevel, nameof(level.Name),
                Resources.Features_InvalidFeature, out var error))
        {
            return error;
        }

        return new Feature(level.Name);
    }

    private Feature(string identifier) : base(identifier.ToLowerInvariant())
    {
    }

    public string Identifier => Value;

    public static ValueObjectFactory<Feature> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Feature(parts[0]!);
        };
    }
}