using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace CarsDomain;

public sealed class TimeSlot : ValueObjectBase<TimeSlot>
{
    public static Result<TimeSlot, Error> Create(DateTime from, DateTime to)
    {
        if (from.IsInvalidParameter(x => x > DateTime.MinValue, nameof(from), out var error1))
        {
            return error1;
        }

        if (to.IsInvalidParameter(x => x > DateTime.MinValue, nameof(to), out var error2))
        {
            return error2;
        }

        if (to.IsInvalidParameter(x => x > from, nameof(to), Resources.TimeSlot_FromDateBeforeToDate, out var error3))
        {
            return error3;
        }

        return new TimeSlot(from, to);
    }

    private TimeSlot(DateTime from, DateTime to)
    {
        From = from;
        To = to;
    }

    public DateTime From { get; }

    public DateTime To { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<TimeSlot> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new TimeSlot(
                parts[0].Value.FromIso8601(),
                parts[1].Value.FromIso8601());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [From, To];
    }
}

public static class TimeSlotExtensions
{
    public static bool IsIntersecting(this TimeSlot first, TimeSlot second)
    {
        return second.IsEncompassing(first)
               || first.IsOverlappedAtStartBy(second)
               || first.IsOverlappedAtEndBy(second)
               || first.IsUnderlappedBy(second);
    }

    public static bool IsOverlapping(this TimeSlot first, TimeSlot second)
    {
        return second.IsEncompassing(first)
               || first.IsOverlappedAtStartBy(second)
               || first.IsOverlappedAtEndBy(second);
    }

    public static bool StartsAfter(this TimeSlot slot, DateTime dateTime)
    {
        return slot.From > dateTime;
    }

    private static bool IsEncompassing(this TimeSlot first, TimeSlot second)
    {
        return first.From <= second.From
               && first.To >= second.To;
    }

    private static bool IsUnderlappedBy(this TimeSlot first, TimeSlot second)
    {
        return second.From >= first.From
               && second.To <= first.To;
    }

    private static bool IsOverlappedAtStartBy(this TimeSlot first, TimeSlot second)
    {
        return second.From < first.From
               && second.To > first.From
               && second.To < first.To;
    }

    private static bool IsOverlappedAtEndBy(this TimeSlot first, TimeSlot second)
    {
        return second.From > first.From
               && second.From < first.To
               && second.To > first.To;
    }
}