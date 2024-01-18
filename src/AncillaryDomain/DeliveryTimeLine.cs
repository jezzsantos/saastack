using Domain.Interfaces;

namespace AncillaryDomain;

using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.ValueObjects;

public sealed class DeliveryTimeLine : SingleValueObjectBase<DeliveryTimeLine, IEnumerable<DateTime>>
{

    public static Result<DeliveryTimeLine, Error> Create()
    {
        return new DeliveryTimeLine(Enumerable.Empty<DateTime>());
    }
    public static Result<DeliveryTimeLine, Error> Create(IEnumerable<DateTime> history, DateTime when)
    {

        return new DeliveryTimeLine(history.Concat(new []{when}));
    }

    private DeliveryTimeLine(IEnumerable<DateTime> value) : base(value)
    {
    }


    public static ValueObjectFactory<DeliveryTimeLine> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true, true);
            return new DeliveryTimeLine(parts[0].Split(",").Select(item=> item.FromIso8601()));
        };
    }

    public Result<DeliveryTimeLine,Error> Attempt(DateTime when)
    {
        return Create(Value, when);
    }
}