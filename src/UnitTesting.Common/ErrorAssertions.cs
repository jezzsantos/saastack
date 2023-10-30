using Common;
using Common.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace UnitTesting.Common;

public class ErrorAssertions : ObjectAssertions<Error, ErrorAssertions>
{
    public ErrorAssertions(Error instance) : base(instance)
    {
    }

    protected override string Identifier => "error";

    public AndConstraint<ErrorAssertions> BeError(ErrorCode code,
        string? message = null, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(error => error.Code == code)
            .FailWith("Expected {context:error} to contain code {0}{reason}, but found {1}.",
                _ => code, error => error.Code);

        if (message.HasValue())
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(error => error.Message == message)
                .FailWith("Expected {context:error} to contain message {0}{reason}, but found {1}.",
                    _ => message, error => error.Message);
        }

        return new AndConstraint<ErrorAssertions>(this);
    }
}