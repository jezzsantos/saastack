using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using FluentAssertions.Primitives;

namespace UnitTesting.Common.Validation;

[ExcludeFromCodeCoverage]
public static class DateTimeExtensions
{
    public static AndConstraint<DateTimeAssertions> BeNear(this DateTimeAssertions assertions, DateTime nearbyTime,
        int precision = 1500, string because = "", params object[] becauseArgs)
    {
        return assertions.BeCloseTo(nearbyTime, TimeSpan.FromMilliseconds(precision), because, becauseArgs);
    }

    public static AndConstraint<DateTimeAssertions> BeNear(this DateTimeAssertions assertions, DateTime nearbyTime,
        TimeSpan precision, string because = "", params object[] becauseArgs)
    {
        return assertions.BeCloseTo(nearbyTime, precision, because, becauseArgs);
    }

    public static AndConstraint<NullableDateTimeAssertions> BeNear(this NullableDateTimeAssertions assertions,
        DateTime nearbyTime, int precision = 1500, string because = "", params object[] becauseArgs)
    {
        return assertions.BeCloseTo(nearbyTime, TimeSpan.FromMilliseconds(precision), because, becauseArgs);
    }

    public static AndConstraint<NullableDateTimeAssertions> BeNear(this NullableDateTimeAssertions assertions,
        DateTime nearbyTime, TimeSpan precision, string because = "", params object[] becauseArgs)
    {
        return assertions.BeCloseTo(nearbyTime, precision, because, becauseArgs);
    }
}