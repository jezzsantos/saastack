using System.Collections;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;

namespace CarsDomain;

public class Unavailabilities : IReadOnlyList<UnavailabilityEntity>
{
    private readonly List<UnavailabilityEntity> _unavailabilities = new();

    public Result<Error> EnsureInvariants()
    {
        _unavailabilities
            .ForEach(una => una.EnsureInvariants());

        if (HasIncompatibleOverlaps())
        {
            return Error.RuleViolation(Resources.Unavailabilities_OverlappingSlot);
        }

        return Result.Ok;
    }

    public IEnumerator<UnavailabilityEntity> GetEnumerator()
    {
        return _unavailabilities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => _unavailabilities.Count;

    public UnavailabilityEntity this[int index] => _unavailabilities[index];

    public void Add(UnavailabilityEntity unavailability)
    {
        var match = FindMatching(unavailability);
        if (match.Exists())
        {
            _unavailabilities.Remove(match);
        }

        _unavailabilities.Add(unavailability);
    }

    public UnavailabilityEntity? FindSlot(TimeSlot slot)
    {
        return _unavailabilities.FirstOrDefault(una => una.Slot.Exists() && una.Slot == slot);
    }

    public void Remove(Identifier unavailabilityId)
    {
        var unavailability = _unavailabilities.Find(una => una.Id == unavailabilityId);
        if (unavailability.Exists())
        {
            _unavailabilities.Remove(unavailability);
        }
    }

    private UnavailabilityEntity? FindMatching(UnavailabilityEntity unavailability)
    {
        return _unavailabilities
            .FirstOrDefault(u =>
                Overlaps(u, unavailability) && !HasDifferentCause(u, unavailability));
    }

    private bool HasIncompatibleOverlaps()
    {
        return _unavailabilities.Any(current =>
            _unavailabilities.Where(next => IsDifferentFrom(current, next))
                .Any(next => InConflict(current, next)));
    }

    private static bool IsDifferentFrom(UnavailabilityEntity current, UnavailabilityEntity next)
    {
        return !next.Equals(current);
    }

    private static bool InConflict(UnavailabilityEntity current, UnavailabilityEntity next)
    {
        return Overlaps(current, next) && HasDifferentCause(current, next);
    }

    private static bool Overlaps(UnavailabilityEntity current, UnavailabilityEntity next)
    {
        if (current.Slot.NotExists())
        {
            return false;
        }

        if (next.Slot.NotExists())
        {
            return false;
        }

        return next.Slot.IsIntersecting(current.Slot);
    }

    private static bool HasDifferentCause(UnavailabilityEntity current, UnavailabilityEntity next)
    {
        return current.IsDifferentCause(next);
    }
}