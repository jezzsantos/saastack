using System.Net;
using Common.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Infrastructure.Web.Api.Common.UnitTests.Endpoints;

internal static class ResultExtensions
{
    public static EndpointTestingAssertions Should(this ProblemHttpResult instance)
    {
        return new EndpointTestingAssertions(instance);
    }
}

internal class EndpointTestingAssertions : ObjectAssertions<ProblemHttpResult, EndpointTestingAssertions>
{
    public EndpointTestingAssertions(ProblemHttpResult instance) : base(instance)
    {
    }

    protected override string Identifier => "response";

    public AndConstraint<EndpointTestingAssertions> BeAProblem(HttpStatusCode status, string? detail,
        string? message = null, string because = "", params object[] becauseArgs)
    {
        ProblemHttpResult? problemHttpResult = null;
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(response =>
            {
                problemHttpResult = GetProblemResult(response);
                if (problemHttpResult.NotExists())
                {
                    return false;
                }

                return problemHttpResult.ProblemDetails.Detail == detail;
            })
            .FailWith(
                "Expected {context:response} to be a ProblemDetails containing Detail {0}{reason}, but found {1}.",
                _ => detail, _ => problemHttpResult.Exists()
                    ? problemHttpResult.ProblemDetails.Detail
                    : "a different kind of response")
            .Then
            .ForCondition(_ =>
            {
                if (problemHttpResult.NotExists())
                {
                    return false;
                }

                return problemHttpResult.StatusCode == (int)status;
            })
            .FailWith("Expected {context:response} to have status code {0}{reason}, but found {1}.",
                _ => status, _ => problemHttpResult.Exists()
                    ? problemHttpResult.StatusCode
                    : "a different status code");

        return new AndConstraint<EndpointTestingAssertions>(this);
    }

    private static ProblemHttpResult? GetProblemResult(object? instance)
    {
        if (instance is ProblemHttpResult result)
        {
            return result;
        }

        return null;
    }
}