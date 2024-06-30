using System.Collections;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;

namespace CarsDomain;

public class Unavailabilities : IReadOnlyList<Unavailability>
{
    private readonly List<Unavailability> _unavailabilities = new();

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

    public int Count => _unavailabilities.Count;

    public IEnumerator<Unavailability> GetEnumerator()
    {
        return _unavailabilities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Unavailability this[int index] => _unavailabilities[index];

    public void Add(Unavailability unavailability)
    {
        var match = FindMatching(unavailability);
        if (match.Exists())
        {
            _unavailabilities.Remove(match);
        }

        _unavailabilities.Add(unavailability);
    }

    public Unavailability? FindSlot(TimeSlot slot)
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

    private Unavailability? FindMatching(Unavailability unavailability)
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

    private static bool IsDifferentFrom(Unavailability current, Unavailability next)
    {
        return !next.Equals(current);
    }

    private static bool InConflict(Unavailability current, Unavailability next)
    {
        return Overlaps(current, next) && HasDifferentCause(current, next);
    }

    private static bool Overlaps(Unavailability current, Unavailability next)
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

    private static bool HasDifferentCause(Unavailability current, Unavailability next)
    {
        return current.IsDifferentCause(next);
    }
}