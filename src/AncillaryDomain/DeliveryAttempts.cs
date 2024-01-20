using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace AncillaryDomain;

public sealed class DeliveryAttempts : SingleValueObjectBase<DeliveryAttempts, List<DateTime>>
{
    public static readonly DeliveryAttempts Empty = new(new List<DateTime>());

    public static Result<DeliveryAttempts, Error> Create(DateTime when)
    {
        return new DeliveryAttempts(new List<DateTime> { when });
    }

    public static Result<DeliveryAttempts, Error> Create(List<DateTime> previousAttempts)
    {
        if (previousAttempts.HasAny())
        {
            DateTime? last = null;
            foreach (var attempt in previousAttempts)
            {
                if (!last.HasValue)
                {
                    last = attempt;
                    continue;
                }

                if (attempt.IsBefore(last.Value))
                {
                    return Error.Validation(Resources.DeliveryAttempts_PreviousAttemptsNotInOrder);
                }

                last = attempt;
            }
        }

        return new DeliveryAttempts(previousAttempts);
    }

    public static Result<DeliveryAttempts, Error> Create(List<DateTime> previousAttempts, DateTime attempt)
    {
        var allAttempts = previousAttempts
            .Concat(new[] { attempt })
            .ToList();

        return Create(allAttempts);
    }

    private DeliveryAttempts(List<DateTime> value) : base(value)
    {
    }

    public IReadOnlyList<DateTime> Attempts => Value;

    public bool HasBeenAttempted => Attempts.HasAny();

    public static ValueObjectFactory<DeliveryAttempts> Rehydrate()
    {
        return (property, _) =>
        {
            var items = RehydrateToList(property, true, true);
            return new DeliveryAttempts(items.Select(item => item.FromIso8601()).ToList());
        };
    }

    public Result<DeliveryAttempts, Error> Attempt(DateTime when)
    {
        if (Value.HasAny())
        {
            if (when.IsInvalidParameter(w => w > Value.Max(), nameof(when),
                    Resources.DeliveryAttempts_LatestAttemptNotAfterLastAttempt, out var error1))
            {
                return error1;
            }
        }

        return Create(Value, when);
    }
}