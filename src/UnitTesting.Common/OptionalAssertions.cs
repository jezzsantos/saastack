using Common;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace UnitTesting.Common;

public class OptionalAssertions<TValue> : ObjectAssertions<Optional<TValue>, OptionalAssertions<TValue>>
{
    public OptionalAssertions(Optional<TValue> value) : base(value)
    {
    }

    protected override string Identifier => "optional";

    public AndConstraint<OptionalAssertions<TValue>> BeNone(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(optional => !optional.HasValue)
            .FailWith(
                "Expected {context:optional} to be None{reason}, but it was {0} instead.",
                optional => optional.ValueOrDefault);

        return new AndConstraint<OptionalAssertions<TValue>>(this);
    }

    public AndConstraint<OptionalAssertions<TValue>> BeSome(TValue some, string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(optional => optional.HasValue && optional.Value!.Equals(some))
            .FailWith(
                "Expected {context:optional} to be value {0}{reason}, but it was {1} instead.", _ => some,
                optional => optional.ValueOrDefault);

        return new AndConstraint<OptionalAssertions<TValue>>(this);
    }

    public AndConstraint<OptionalAssertions<TValue>> BeSome(Predicate<TValue> some, string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(optional => optional.HasValue && some(optional))
            .FailWith(
                "Expected {context:optional} to be value {0}{reason}, but it was {1} instead.",
                optional => some(optional),
                optional => optional.ValueOrDefault);

        return new AndConstraint<OptionalAssertions<TValue>>(this);
    }

    public AndConstraint<OptionalAssertions<TValue>> NotBeNone(string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(optional => optional.HasValue)
            .FailWith(
                "Expected {context:optional} not to be None{reason}, but it was.",
                optional => optional.ValueOrDefault);

        return new AndConstraint<OptionalAssertions<TValue>>(this);
    }
}