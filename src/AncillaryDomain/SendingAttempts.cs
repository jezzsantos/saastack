using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace AncillaryDomain;

public sealed class SendingAttempts : SingleValueObjectBase<SendingAttempts, List<DateTime>>
{
    public static readonly SendingAttempts Empty = new(new List<DateTime>());

    public static Result<SendingAttempts, Error> Create(DateTime when)
    {
        return new SendingAttempts([when]);
    }

    public static Result<SendingAttempts, Error> Create(List<DateTime> previousAttempts)
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
                    return Error.Validation(Resources.SendingAttempts_PreviousAttemptsNotInOrder);
                }

                last = attempt;
            }
        }

        return new SendingAttempts(previousAttempts);
    }

    public static Result<SendingAttempts, Error> Create(List<DateTime> previousAttempts, DateTime attempt)
    {
        var allAttempts = previousAttempts
            .Concat(new[] { attempt })
            .ToList();

        return Create(allAttempts);
    }

    private SendingAttempts(List<DateTime> value) : base(value)
    {
    }

    public IReadOnlyList<DateTime> Attempts => Value;

    public bool HasBeenAttempted => Attempts.HasAny();

    public static ValueObjectFactory<SendingAttempts> Rehydrate()
    {
        return (property, _) =>
        {
            var items = RehydrateToList(property, true, true);
            return new SendingAttempts(items.Select(item => item.FromIso8601()).ToList());
        };
    }

    public Result<SendingAttempts, Error> Attempt(DateTime when)
    {
        if (Value.HasAny())
        {
            if (when.IsInvalidParameter(w => w > Value.Max(), nameof(when),
                    Resources.SendingAttempts_LatestAttemptNotAfterLastAttempt, out var error1))
            {
                return error1;
            }
        }

        return Create(Value, when);
    }
}