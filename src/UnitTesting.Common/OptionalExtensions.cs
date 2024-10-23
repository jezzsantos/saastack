using Common;
using FluentAssertions;
using FluentAssertions.Primitives;
using UnitTesting.Common.Validation;

namespace UnitTesting.Common;

public static class OptionalExtensions
{
    public static AndConstraint<DateTimeAssertions> BeNear(this OptionalAssertions<DateTime> assertions,
        Optional<DateTime> nearbyTime, int precision = 850, string because = "", params object[] becauseArgs)
    {
        return new DateTimeAssertions(assertions.Subject.HasValue
                ? assertions.Subject.Value
                : null)
            .BeNear(nearbyTime, TimeSpan.FromMilliseconds(precision), because, becauseArgs);
    }

    public static OptionalAssertions<TValue> Should<TValue>(this Optional<TValue> instance)
    {
        return new OptionalAssertions<TValue>(instance);
    }
}