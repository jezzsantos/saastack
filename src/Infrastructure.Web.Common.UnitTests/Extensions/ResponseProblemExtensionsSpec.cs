using System.Net;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces.Clients;
using Microsoft.AspNetCore.Mvc;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class ResponseProblemExtensionsSpec
{
    [Fact]
    public void WhenToExceptionAndNoDetail_ThenReturnsException()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = 999
        }.ToException();

        result.Message.Should().Be("999: atitle");
    }

    [Fact]
    public void WhenToExceptionAndDetail_ThenReturnsException()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = 999,
            Detail = "adetail"
        }.ToException();

        result.Message.Should().Be("999: atitle, adetail");
    }

    [Fact]
    public void WhenToExceptionAndException_ThenReturnsException()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = 999,
            Detail = "adetail",
            Exception = "anexception"
        }.ToException();

        result.Message.Should().Be("999: atitle, adetail, anexception");
    }

    [Fact]
    public void WhenToResponseProblemAndNoProblem_ThenReturnsEmptyProblem()
    {
        var result = ((ProblemDetails?)null).ToResponseProblem();

        result.Title.Should().Be(nameof(HttpStatusCode.InternalServerError));
        result.Status.Should().Be((int)HttpStatusCode.InternalServerError);
        result.Type.Should().BeNull();
        result.Detail.Should().BeNull();
        result.Errors.Should().BeNull();
        result.Exception.Should().BeNull();
        result.Instance.Should().BeNull();
    }

    [Fact]
    public void WhenToResponseProblemHasException_ThenReturnsProblem()
    {
        var result = new ProblemDetails
        {
            Type = "atype",
            Title = "atitle",
            Status = 999,
            Detail = "adetail",
            Instance = "aninstance",
            Extensions = { { HttpConstants.Responses.ProblemDetails.Extensions.ExceptionPropertyName, "anexception" } }
        }.ToResponseProblem();

        result.Title.Should().Be("atitle");
        result.Status.Should().Be(999);
        result.Type.Should().Be("atype");
        result.Detail.Should().Be("adetail");
        result.Errors.Should().BeNull();
        result.Exception.Should().Be("anexception");
        result.Instance.Should().Be("aninstance");
    }

    [Fact]
    public void WhenToResponseProblemHasValidationErrors_ThenReturnsProblem()
    {
        var errors = new List<ValidatorProblem>
        {
            new()
            {
                Reason = "areason",
                Rule = "arule",
                Value = "avalue"
            }
        };
        var result = new ProblemDetails
        {
            Type = "atype",
            Title = "atitle",
            Status = 999,
            Detail = "adetail",
            Instance = "aninstance",
            Extensions =
                { { HttpConstants.Responses.ProblemDetails.Extensions.ValidationErrorPropertyName, errors.ToJson() } }
        }.ToResponseProblem();

        result.Title.Should().Be("atitle");
        result.Status.Should().Be(999);
        result.Type.Should().Be("atype");
        result.Detail.Should().Be("adetail");
        result.Errors.Should()
            .ContainSingle(x => x.Reason == "areason" && x.Rule == "arule" && x.Value!.ToString() == "avalue");
        result.Exception.Should().BeNull();
        result.Instance.Should().Be("aninstance");
    }

    [Fact]
    public void WhenToErrorAndNoStatus_ThenReturnsUnexpectedError()
    {
        var result = new ResponseProblem().ToError();

        result.Should().BeError(ErrorCode.Unexpected);
    }

    [Fact]
    public void WhenToErrorAndHas500Status_ThenReturnsUnexpectedError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.InternalServerError
        }.ToError();

        result.Should().BeError(ErrorCode.Unexpected, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas400StatusAndNoValidationErrors_ThenReturnsRuleViolationError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.BadRequest
        }.ToError();

        result.Should().BeError(ErrorCode.RuleViolation, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas400StatusAndValidationErrors_ThenReturnsValidationError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.BadRequest,
            Errors = new ValidatorProblem[]
            {
                new()
                {
                    Reason = "areason1",
                    Rule = "arule1",
                    Value = "avalue1"
                },
                new()
                {
                    Reason = "areason2",
                    Rule = "arule2",
                    Value = "avalue2"
                }
            }
        }.ToError();

        result.Should().BeError(ErrorCode.Validation, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas401Status_ThenReturnsNotAuthenticatedError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.Unauthorized
        }.ToError();

        result.Should().BeError(ErrorCode.NotAuthenticated, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas403Status_ThenReturnsForbiddenAccessError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.Forbidden
        }.ToError();

        result.Should().BeError(ErrorCode.ForbiddenAccess, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas404Status_ThenReturnsEntityNotFoundError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.NotFound
        }.ToError();

        result.Should().BeError(ErrorCode.EntityNotFound, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas405Status_ThenReturnsPreconditionError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.MethodNotAllowed
        }.ToError();

        result.Should().BeError(ErrorCode.PreconditionViolation, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas409Status_ThenReturnsEntityExistsError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.Conflict
        }.ToError();

        result.Should().BeError(ErrorCode.EntityExists, "atitle");
    }

    [Fact]
    public void WhenToErrorAndHas423Status_ThenReturnsEntityLockedError()
    {
        var result = new ResponseProblem
        {
            Title = "atitle",
            Status = (int)HttpStatusCode.Locked
        }.ToError();

        result.Should().BeError(ErrorCode.EntityLocked, "atitle");
    }

    [Fact]
    public void WhenToResponseProblemAndTitleIsNull_ThenReturnsProblem()
    {
        var result = HttpStatusCode.InternalServerError.ToResponseProblem(null);

        result.Title.Should().Be(nameof(HttpStatusCode.InternalServerError));
        result.Status.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void WhenToResponseProblemAndHasTitle_ThenReturnsProblem()
    {
        var result = HttpStatusCode.InternalServerError.ToResponseProblem("atitle");

        result.Title.Should().Be("atitle");
        result.Status.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void WhenToResponseProblemAndHasDetail_ThenReturnsProblem()
    {
        var result = HttpStatusCode.InternalServerError.ToResponseProblem("atitle", "adetail");

        result.Title.Should().Be("atitle");
        result.Detail.Should().Be("adetail");
        result.Status.Should().Be((int)HttpStatusCode.InternalServerError);
    }    
    [Fact]
    public void WhenToResponseProblemAndHasType_ThenReturnsProblem()
    {
        var result = HttpStatusCode.InternalServerError.ToResponseProblem("atitle", null, "atype");

        result.Title.Should().Be("atitle");
        result.Detail.Should().BeNull();
        result.Type.Should().Be("atype");
        result.Status.Should().Be((int)HttpStatusCode.InternalServerError);
    }
}