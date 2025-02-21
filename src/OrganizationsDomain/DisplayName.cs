using Common;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace OrganizationsDomain;

public sealed class DisplayName : SingleValueObjectBase<DisplayName, string>
{
    public static readonly DisplayName Empty = new(string.Empty);

    public static Result<DisplayName, Error> Create(string value)
    {
        if (value.IsInvalidParameter(Validations.DisplayName, nameof(value),
                Resources.OrganizationDisplayName_InvalidName, out var error))
        {
            return error;
        }

        return new DisplayName(value);
    }

    private DisplayName(string name) : base(name)
    {
    }

    public string Name => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<DisplayName> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new DisplayName(parts[0]!);
        };
    }
}