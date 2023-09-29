using Common.Extensions;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace Common.UnitTests;

[UsedImplicitly]
public class ResultSpec
{
    [Trait("Category", "Unit")]
    public class ResultWithValueSpec
    {
        [Fact]
        public void WhenConstructedWithNullScalarValue_ThenInitialized()
        {
            var result = new Result<string, TestError>(null!);

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void WhenConstructedWithAnyScalarValue_ThenInitialized()
        {
            var result = new Result<string, TestError>(string.Empty);

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void WhenConstructedWithEmptyOptionalValue_ThenInitialized()
        {
            var result = new Result<string, TestError>(new Optional<string>());

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void WhenConstructedWithAnyOptionalValue_ThenInitialized()
        {
            var result = new Result<string, TestError>(new Optional<string>(string.Empty));

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void WhenConstructedWithAnyError_ThenInitialized()
        {
            var result = new Result<string, TestError>(new TestError());

            result.IsSuccessful.Should().BeFalse();
        }

        [Fact]
        public void WhenGetValueAndFaulted_ThenThrows()
        {
            var result = new Result<string, TestError>(new TestError());

            result.Invoking(x => x.Value).Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.Result_FetchValueWhenFaulted.Format("atesterror"));
        }

        [Fact]
        public void WhenGetValue_ThenReturnsValue()
        {
            var result = new Result<string, TestError>("avalue");

            var actual = result.Value;

            actual.Should().Be("avalue");
        }

        [Fact]
        public void WhenGetErrorAndNotFaulted_ThenThrows()
        {
            var result = new Result<string, TestError>("avalue");

            result.Invoking(x => x.Error).Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.Result_FetchErrorWhenNotFaulted);
        }

        [Fact]
        public void WhenGetError_ThenReturnsValue()
        {
            var error = new TestError();
            var result = new Result<string, TestError>(error);

            var actual = result.Error;

            actual.Should().Be(error);
        }

        [Fact]
        public void WhenHasValueAndFaulted_ThenReturnsFalse()
        {
            var result = new Result<string, TestError>(new TestError());

            var actual = result.HasValue;

            actual.Should().BeFalse();
        }

        [Fact]
        public void WhenHasValueAndNotFaultedWithNull_ThenReturnsFalse()
        {
            var result = new Result<string, TestError>(null!);

            var actual = result.HasValue;

            actual.Should().BeFalse();
        }

        [Fact]
        public void WhenHasValueAndNotFaultedWithValue_ThenReturnsTrue()
        {
            var result = new Result<string, TestError>("avalue");

            var actual = result.HasValue;

            actual.Should().BeTrue();
        }

        [Fact]
        public void WhenExistsAndFaulted_ThenReturnsFalse()
        {
            var result = new Result<string, TestError>(new TestError());

            var actual = result.Exists;

            actual.Should().BeFalse();
        }

        [Fact]
        public void WhenExistsAndNotFaultedWithNull_ThenReturnsFalse()
        {
            var result = new Result<string, TestError>(null!);

            var actual = result.Exists;

            actual.Should().BeFalse();
        }

        [Fact]
        public void WhenExistsAndNotFaultedWithValue_ThenReturnsTrue()
        {
            var result = new Result<string, TestError>("avalue");

            var actual = result.Exists;

            actual.Should().BeTrue();
        }

        [Fact]
        public void WhenFromErrorWithError_ThenReturnsResultOfError()
        {
            var error = new TestError();

            var result = Result<string, TestError>.FromError(error);

            result.Error.Should().Be(error);
        }

        [Fact]
        public void WhenFromValueWithValue_ThenReturnsResultOfValue()
        {
            var result = Result<string, TestError>.FromResult("avalue");

            result.Value.Should().Be("avalue");
        }

        [Fact]
        public void WhenTryGetOutAndIsFaulted_ThenReturnsFalse()
        {
            var error = new TestError();
            var result = new Result<string, TestError>(error);

            var actual = result.TryGet(out var assigned);

            actual.Should().BeFalse();
            assigned.Should().Be(null);
        }

        [Fact]
        public void WhenTryGetOutAndIsNotFaulted_ThenReturnsTrue()
        {
            var result = new Result<string, TestError>("avalue");

            var actual = result.TryGet(out var assigned);

            actual.Should().BeTrue();
            assigned.Should().Be("avalue");
        }

        [Fact]
        public void WhenTryGetErrorAndIsNotFaulted_ThenReturnsFalse()
        {
            var result = new Result<string, TestError>("avalue");

            var actual = result.TryGetError(out var assigned);

            actual.Should().BeFalse();
            assigned.Should().Be(null);
        }

        [Fact]
        public void WhenTryGetErrorAndIsFaulted_ThenReturnsTrue()
        {
            var error = new TestError();
            var result = new Result<string, TestError>(error);

            var actual = result.TryGetError(out var assigned);

            actual.Should().BeTrue();
            assigned.Should().Be(error);
        }

        [Fact]
        public void WhenToStringAndFaulted_ThenReturnsErrorRepresentation()
        {
            var error = new TestError();
            var result = new Result<string, TestError>(error);

            var actual = result.ToString();

            actual.Should().Be("atesterror");
        }

        [Fact]
        public void WhenToStringAndNotFaulted_ThenReturnsValueRepresentation()
        {
            var result = new Result<string, TestError>("avalue");

            var actual = result.ToString();

            actual.Should().Be("avalue");
        }

        [Fact]
        public void WhenCastAnyValueToResultOfThatValue_ThenReturnsResultOfThatValue()
        {
            var result = (Result<string, TestError>)"avalue";

            result.IsSuccessful.Should().BeTrue();
            result.Value.Should().Be("avalue");
        }

        [Fact]
        public void WhenCastAnyErrorToResultOfThatError_ThenReturnsResultOfThatError()
        {
            var error = new TestError();

            var result = (Result<string, TestError>)error;

            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(error);
        }

        [Fact]
        public void WhenCastAnyResultOfValueToTypeOfValue_ThenReturnsValue()
        {
            var result = new Result<string, TestError>("avalue");

            var actual = (string)result;

            actual.Should().Be("avalue");
        }

        [Fact]
        public void WhenBitwiseAndResultsThatAreNotBothSuccessful_ThenReturnsFalse()
        {
            var result1 = new Result<string, TestError>(new TestError());
            var result2 = new Result<string, TestError>("avalue2");

            var actual = result1 & result2;

            actual.Should().BeFalse();
        }

        [Fact]
        public void WhenBitwiseAndingResultsThatAreBothSuccessful_ThenReturnsTrue()
        {
            var result1 = new Result<string, TestError>("avalue1");
            var result2 = new Result<string, TestError>("avalue2");

            var actual = result1 & result2;

            actual.Should().BeTrue();
        }

        [Fact]
        public void WhenMatchAndSuccessfulContainingNullValue_ThenReturnsOptionalNone()
        {
            var result = new Result<string, TestError>();
            var successWasCalled = false;
            var errorWasCalled = false;
            object? passedValue = null;

            var match = result.Match(success =>
            {
                successWasCalled = true;
                passedValue = success;
                return true;
            }, fail =>
            {
                errorWasCalled = true;
                passedValue = fail;
                return false;
            });

            match.Should().BeTrue();
            successWasCalled.Should().BeTrue();
            errorWasCalled.Should().BeFalse();
            passedValue.Should().Be(Optional<string>.None);
        }

        [Fact]
        public void WhenMatchAndSuccessfulContainingNonNullValue_ThenReturnsTheOptionalValue()
        {
            var result = new Result<string, TestError>("avalue");
            var successWasCalled = false;
            var errorWasCalled = false;
            object? passedValue = null;

            var match = result.Match(success =>
            {
                successWasCalled = true;
                passedValue = success;
                return true;
            }, fail =>
            {
                errorWasCalled = true;
                passedValue = fail;
                return false;
            });

            match.Should().BeTrue();
            successWasCalled.Should().BeTrue();
            errorWasCalled.Should().BeFalse();
            passedValue.Should().Be("avalue");
        }

        [Fact]
        public void WhenMatchAndNotSuccessful_ThenReturnsTheError()
        {
            var error = new TestError();
            var result = new Result<string, TestError>(error);
            var successWasCalled = false;
            var errorWasCalled = false;
            object? passedValue = null;

            var match = result.Match(success =>
            {
                successWasCalled = true;
                passedValue = success;
                return false;
            }, fail =>
            {
                errorWasCalled = true;
                passedValue = fail;
                return true;
            });

            match.Should().BeTrue();
            successWasCalled.Should().BeFalse();
            errorWasCalled.Should().BeTrue();
            passedValue.Should().Be(error);
        }
    }

    [Trait("Category", "Unit")]
    public class ResultWithoutValueSpec
    {
        [Fact]
        public void WhenConstructedWithNoValue_ThenInitialized()
        {
            var result = new Result<TestError>();

            result.IsSuccessful.Should().BeTrue();
        }

        [Fact]
        public void WhenConstructedWithAnyError_ThenInitialized()
        {
            var result = new Result<TestError>(new TestError());

            result.IsSuccessful.Should().BeFalse();
        }

        [Fact]
        public void WhenGetErrorAndNotFaulted_ThenThrows()
        {
            var result = new Result<TestError>();

            result.Invoking(x => x.Error).Should().Throw<InvalidOperationException>()
                .WithMessage(Resources.Result_FetchErrorWhenNotFaulted);
        }

        [Fact]
        public void WhenGetError_ThenReturnsValue()
        {
            var error = new TestError();
            var result = new Result<TestError>(error);

            var actual = result.Error;

            actual.Should().Be(error);
        }

        [Fact]
        public void WhenFromErrorWithError_ThenReturnsResultOfError()
        {
            var error = new TestError();

            var result = Result<TestError>.FromError(error);

            result.Error.Should().Be(error);
        }

        [Fact]
        public void WhenTryGetErrorAndIsNotFaulted_ThenReturnsFalse()
        {
            var result = new Result<TestError>();

            var actual = result.TryGetError(out var assigned);

            actual.Should().BeFalse();
            assigned.Should().Be(null);
        }

        [Fact]
        public void WhenTryGetErrorAndIsFaulted_ThenReturnsTrue()
        {
            var error = new TestError();
            var result = new Result<TestError>(error);

            var actual = result.TryGetError(out var assigned);

            actual.Should().BeTrue();
            assigned.Should().Be(error);
        }

        [Fact]
        public void WhenToStringAndFaulted_ThenReturnsErrorRepresentation()
        {
            var error = new TestError();
            var result = new Result<TestError>(error);

            var actual = result.ToString();

            actual.Should().Be("atesterror");
        }

        [Fact]
        public void WhenToStringAndNotFaulted_ThenReturnsValueRepresentation()
        {
            var result = new Result<TestError>();

            var actual = result.ToString();

            actual.Should().Be("OK");
        }

        [Fact]
        public void WhenCastAnyErrorToResultOfThatError_ThenReturnsResultOfThatError()
        {
            var error = new TestError();

            var result = (Result<TestError>)error;

            result.IsSuccessful.Should().BeFalse();
            result.Error.Should().Be(error);
        }

        [Fact]
        public void WhenBitwiseAndResultsThatAreNotBothSuccessful_ThenReturnsFalse()
        {
            var result1 = new Result<TestError>(new TestError());
            var result2 = new Result<TestError>();

            var actual = result1 & result2;

            actual.Should().BeFalse();
        }

        [Fact]
        public void WhenBitwiseAndingResultsThatAreBothSuccessful_ThenReturnsTrue()
        {
            var result1 = new Result<TestError>();
            var result2 = new Result<TestError>();

            var actual = result1 & result2;

            actual.Should().BeTrue();
        }

        [Fact]
        public void WhenMatchAndSuccessful_ThenReturnsTrue()
        {
            var result = new Result<TestError>();
            var successWasCalled = false;
            var errorWasCalled = false;
            object? passedValue = null;

            var match = result.Match(() =>
            {
                successWasCalled = true;
                passedValue = null;
                return true;
            }, fail =>
            {
                errorWasCalled = true;
                passedValue = fail;
                return false;
            });

            match.Should().BeTrue();
            successWasCalled.Should().BeTrue();
            errorWasCalled.Should().BeFalse();
            passedValue.Should().BeNull();
        }

        [Fact]
        public void WhenMatchAndNotSuccessful_ThenReturnsTheError()
        {
            var error = new TestError();
            var result = new Result<TestError>(error);
            var successWasCalled = false;
            var errorWasCalled = false;
            object? passedValue = null;

            var match = result.Match(() =>
            {
                successWasCalled = true;
                passedValue = null;
                return false;
            }, fail =>
            {
                errorWasCalled = true;
                passedValue = fail;
                return true;
            });

            match.Should().BeTrue();
            successWasCalled.Should().BeFalse();
            errorWasCalled.Should().BeTrue();
            passedValue.Should().Be(error);
        }
    }
}

public struct TestError
{
    public override string ToString()
    {
        return "atesterror";
    }
}