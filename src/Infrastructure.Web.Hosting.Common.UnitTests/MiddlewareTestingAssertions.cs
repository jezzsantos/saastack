using System.Net;
using System.Text;
using Common.Extensions;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Hosting.Common.UnitTests;

internal static class ResultExtensions
{
    public static MiddlewareTestingAssertions Should(this HttpResponse instance)
    {
        return new MiddlewareTestingAssertions(instance);
    }
}

internal class MiddlewareTestingAssertions : ObjectAssertions<HttpResponse, MiddlewareTestingAssertions>
{
    public MiddlewareTestingAssertions(HttpResponse instance) : base(instance)
    {
    }

    protected override string Identifier => "response";

    public AndConstraint<MiddlewareTestingAssertions> BeAProblem(HttpStatusCode status, string? detail,
        string? message = null, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(response => response.StatusCode == (int)status)
            .FailWith("Expected {context:response} to have status code {0}{reason}, but found {1}.",
                _ => status, response => response.StatusCode);

        if (detail.HasValue())
        {
            ProblemDetails? problemDetails = null;
            Execute.Assertion
                .BecauseOf(because, becauseArgs)
                .Given(() => Subject)
                .ForCondition(response =>
                {
                    problemDetails = GetProblemDetails(response);

                    if (problemDetails.NotExists())
                    {
                        return false;
                    }

                    return problemDetails.Detail == detail;
                })
                .FailWith(
                    "Expected {context:response} to be a ProblemDetails containing Detail {0}{reason}, but found {1}.",
                    _ => detail, _ => problemDetails.Exists()
                        ? problemDetails.Detail
                        : "a different kind of response");
        }

        return new AndConstraint<MiddlewareTestingAssertions>(this);
    }

    public AndConstraint<MiddlewareTestingAssertions> NotBeAProblem(string? message = null, string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .Given(() => Subject)
            .ForCondition(response => response.StatusCode == 200)
            .FailWith("Expected {context:response} to have status code 200{reason}, but found {0}.",
                response => response.StatusCode);

        return new AndConstraint<MiddlewareTestingAssertions>(this);
    }

    private static ProblemDetails? GetProblemDetails(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        var body = response.Body.ReadFully();

        var problem = Encoding.UTF8.GetString(body).FromJson<ProblemDetails>();

        return problem;
    }
}