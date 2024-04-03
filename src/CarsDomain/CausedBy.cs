using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Shared.Cars;

namespace CarsDomain;

public sealed class CausedBy : ValueObjectBase<CausedBy>
{
    public static Result<CausedBy, Error> Create(UnavailabilityCausedBy reason, Optional<string> reference)
    {
        if (reference.HasValue)
        {
            if (reference.Value.IsInvalidParameter(Validations.Unavailability.Reference, nameof(reference),
                    Resources.CausedBy_InvalidReference, out var error1))
            {
                return error1;
            }
        }
        else
        {
            if (reason.IsInvalidParameter(r => r != UnavailabilityCausedBy.Reservation,
                    nameof(reference), Resources.CausedBy_ReservationWithoutReference, out var error2))
            {
                return error2;
            }
        }

        return new CausedBy(reason, reference);
    }

    private CausedBy(UnavailabilityCausedBy reason, Optional<string> reference)
    {
        Reason = reason;
        Reference = reference.ValueOrDefault;
    }

    public UnavailabilityCausedBy Reason { get; }

    public string? Reference { get; }

    public static ValueObjectFactory<CausedBy> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new CausedBy(parts[0]!.ToEnum<UnavailabilityCausedBy>(), parts[1]);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[] { Reason, Reference };
    }
}