using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Validations;
using Domain.Interfaces.ValueObjects;

namespace Domain.Shared;

/// <summary>
///     Defines the name of a feature. We store the name of the <see cref="FeatureLevel" /> only for serialization purposes
/// </summary>
public sealed class Feature : SingleValueObjectBase<Feature, string>
{
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

    [SkipImmutabilityCheck]
    public FeatureLevel AsLevel()
    {
        var knownPlatform = PlatformFeatures.FindFeatureByName(Identifier);
        if (knownPlatform.Exists())
        {
            return knownPlatform;
        }

        var knownTenant = TenantFeatures.FindFeatureByName(Identifier);
        return knownTenant.Exists()
            ? knownTenant
            : new FeatureLevel(Identifier);
    }
}