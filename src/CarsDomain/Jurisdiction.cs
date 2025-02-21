using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace CarsDomain;

public sealed class Jurisdiction : SingleValueObjectBase<Jurisdiction, string>
{
    public static readonly IReadOnlyList<string> AllowedCountries = new List<string> { "New Zealand", "Australia" };

    public static Result<Jurisdiction, Error> Create(string name)
    {
        if (name.IsNotValuedParameter(nameof(name), out var error1))
        {
            return error1;
        }

        if (name.IsInvalidParameter(val => AllowedCountries.Contains(val), nameof(name),
                Resources.Jurisdiction_UnknownJurisdiction, out var error2))
        {
            return error2;
        }

        return new Jurisdiction(name);
    }

    private Jurisdiction(string name) : base(name)
    {
    }

    public string Name => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Jurisdiction> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new Jurisdiction(parts[0]!);
        };
    }
}