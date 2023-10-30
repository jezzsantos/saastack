using Common;
using Common.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace UnitTesting.Common;

public class ResultAssertions : ObjectAssertions<Result<Error>, ResultAssertions>
{
    public ResultAssertions(Result<Error> value) : base(value)
    {
    }

    protected override string Identifier => "result";

    public AndConstraint<ResultAssertions> BeError(ErrorCode code,
        string? message = null, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(result => !result.IsSuccessful)
            .FailWith(
                "Expected {context:result} to return an Error with code {0}{reason}, but it returned a Successful value.",
                _ => code)
            .Then
            .Given(_ => Subject.Error)
            .ForCondition(result => result.Code == code)
            .FailWith("Expected {context:result} to contain code {0}{reason}, but found {1}.",
                _ => code, error => error.Code);

        if (message.HasValue())
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(result => result.Error.Message == message)
                .FailWith("Expected {context:result} to contain message {0}{reason}, but found {1}.",
                    _ => message, result => result.Error.Message);
        }

        return new AndConstraint<ResultAssertions>(this);
    }

    public AndConstraint<ResultAssertions> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(result => result.IsSuccessful)
            .FailWith(
                "Expected {context:result} to return a Successful value {reason}, but it returned an Error with code {0}.",
                result => !result.IsSuccessful
                    ? result.Error.Code
                    : ErrorCode.NoError);

        return new AndConstraint<ResultAssertions>(this);
    }
}

public class ResultAssertions<TValue> : ObjectAssertions<Result<TValue, Error>, ResultAssertions<TValue>>
{
    public ResultAssertions(Result<TValue, Error> instance) : base(instance)
    {
    }

    protected override string Identifier => "result";

    public AndConstraint<ResultAssertions<TValue>> BeError(ErrorCode code,
        string? message = null, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(result => !result.IsSuccessful)
            .FailWith(
                "Expected {context:result} to return an Error with code {0}{reason}, but it returned a Successful value.",
                _ => code)
            .Then
            .Given(_ => Subject.Error)
            .ForCondition(result => result.Code == code)
            .FailWith("Expected {context:result} to contain code {0}{reason}, but found {1}.",
                _ => code, error => error.Code);

        if (message.HasValue())
        {
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(result => result.Error.Message == message)
                .FailWith("Expected {context:result} to contain message {0}{reason}, but found {1}.",
                    _ => message, result => result.Error.Message);
        }

        return new AndConstraint<ResultAssertions<TValue>>(this);
    }

    public AndConstraint<ResultAssertions<TValue>> BeSuccess(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(result => result.IsSuccessful)
            .FailWith(
                "Expected {context:result} to return a Successful value {reason}, but it returned an Error with code {0}, and message {1}.",
                result => !result.IsSuccessful
                    ? result.Error.Code
                    : ErrorCode.NoError, result => !result.IsSuccessful
                    ? result.Error.Message ?? "empty"
                    : string.Empty);

        return new AndConstraint<ResultAssertions<TValue>>(this);
    }
}