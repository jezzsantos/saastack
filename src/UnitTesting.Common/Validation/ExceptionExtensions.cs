using System.Diagnostics.CodeAnalysis;
using Common.Extensions;
using FluentAssertions.Execution;
using FluentAssertions.Specialized;

namespace UnitTesting.Common.Validation;

// ReSharper disable once CheckNamespace
[ExcludeFromCodeCoverage]
public static class ExceptionExtensions
{
    public static ExceptionAssertions<TException> WithMessageLike<TException>(
        this ExceptionAssertions<TException> @throw, string messageWithFormatters, string because = "",
        params object[] becauseArgs)
        where TException : Exception
    {
        if (messageWithFormatters.HasValue())
        {
            var exception = @throw.Subject.Single();
            var expectedFormat = messageWithFormatters.Replace("{", "{{")
                .Replace("}", "}}");
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(IsFormattedFrom(exception.Message, messageWithFormatters))
                .UsingLineBreaks.FailWith(
                    "Expected exception message to match the equivalent of\n\"{0}\", but\n\"{1}\" does not.",
                    () => expectedFormat, () => exception.Message);
        }

        return new ExceptionAssertions<TException>(@throw.Subject);
    }

    public static async Task<ExceptionAssertions<TException>> WithMessageLike<TException>(
        this Task<ExceptionAssertions<TException>> @throw, string messageWithFormatters, string because = "",
        params object[] becauseArgs)
        where TException : Exception
    {
        if (messageWithFormatters.HasValue())
        {
            var exception = (await @throw).Subject.Single();
            var expectedFormat = messageWithFormatters.Replace("{", "{{")
                .Replace("}", "}}");
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .ForCondition(IsFormattedFrom(exception.Message, messageWithFormatters))
                .UsingLineBreaks.FailWith(
                    "Expected exception message to match the equivalent of\n\"{0}\", but\n\"{1}\" does not.",
                    () => expectedFormat, () => exception.Message);
        }

        return new ExceptionAssertions<TException>((await @throw).Subject);
    }

    private static bool IsFormattedFrom(string actualExceptionMessage, string expectedMessageWithFormatters)
    {
        var escapedPattern = expectedMessageWithFormatters.Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("(", "\\(")
            .Replace(")", "\\)")
            .Replace(".", "\\.")
            .Replace("<", "\\<")
            .Replace(">", "\\>");

        var pattern = escapedPattern.ReplaceWith(@"\{\d+\}", ".*")
            .Replace(" ", @"\s");

        return actualExceptionMessage.IsMatchWith(pattern);
    }
}