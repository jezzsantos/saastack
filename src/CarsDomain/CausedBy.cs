using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;

namespace CarsDomain;

public sealed class CausedBy : ValueObjectBase<CausedBy>
{
    public static Result<CausedBy, Error> Create(UnavailabilityCausedBy reason, string? reference)
    {
        if (reference.HasValue())
        {
            if (reference.IsInvalidParameter(Validations.Unavailability.Reference, nameof(reference),
                    Resources.CausedBy_InvalidReference, out var error1))
            {
                return error1;
            }
        }
        else
        {
            if (reference.IsInvalidParameter(_ => reason != UnavailabilityCausedBy.Reservation,
                    nameof(reference), Resources.CausedBy_ReservationWithoutReference, out var error2))
            {
                return error2;
            }
        }

        return new CausedBy(reason, reference);
    }

    private CausedBy(UnavailabilityCausedBy reason, string? reference)
    {
        Reason = reason;
        Reference = reference;
    }

    public static ValueObjectFactory<CausedBy> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new CausedBy(parts[0].ToEnum<UnavailabilityCausedBy>(), parts[1]);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[] { Reason, Reference };
    }

    public UnavailabilityCausedBy Reason { get; }

    public string? Reference { get; }
}