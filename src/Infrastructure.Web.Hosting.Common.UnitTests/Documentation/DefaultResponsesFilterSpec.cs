using System.Reflection;
using Common;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Documentation;
using Infrastructure.Web.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Documentation;

[Trait("Category", "Unit")]
public class DefaultResponsesFilterSpec
{
    private readonly OperationFilterContext _context;
    private readonly DefaultResponsesFilter _filter;
    private readonly Mock<MethodInfo> _methodInfo;
    private readonly OpenApiOperation _operation;
    private readonly Mock<ISchemaGenerator> _schemaGenerator;

    public DefaultResponsesFilterSpec()
    {
        _operation = new OpenApiOperation
        {
            OperationId = "anoperationid",
            Responses = new OpenApiResponses()
        };
        _schemaGenerator = new Mock<ISchemaGenerator>();
        _schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(ProblemDetails), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "ProblemDetails" });
        _methodInfo = new Mock<MethodInfo>();

        _context = new OperationFilterContext(new ApiDescription(), _schemaGenerator.Object, new SchemaRepository(),
            _methodInfo.Object);
        _filter = new DefaultResponsesFilter();
    }

    [Fact]
    public void WhenApplyAndIsNotWebRequest_ThenDoesNothing()
    {
        _filter.Apply(_operation, _context);

        _operation.Responses.Should().BeEmpty();
    }

    [Fact]
    public void WhenApply_ThenSetsDefaultResponses()
    {
        var request = (object _, TestPostRequestVoidResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2);
        _operation.Responses["200"].Description.Should().Be("OK");
        _operation.Responses["200"].Content.Should().BeNull();
        _operation.Responses["400"].Content.Count.Should().Be(1);
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem].Schema.Type.Should()
            .Be("ProblemDetails");
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem].Examples.Count.Should().Be(2);
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem]
            .Examples[ErrorCode.Validation.ToString()].Description.Should()
            .Be(Resources.DefaultResponsesFilter_Example_ValidationError_Description);
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem]
            .Examples[ErrorCode.Validation.ToString()].Summary.Should()
            .Be(Resources.DefaultResponsesFilter_Example_ValidationError_Summary);
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem]
            .Examples[ErrorCode.Validation.ToString()].Value.Should().NotBeNull();
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem]
            .Examples[ErrorCode.RuleViolation.ToString()].Description.Should()
            .Be(Resources.DefaultResponsesFilter_Example_RuleViolationError_Description);
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem]
            .Examples[ErrorCode.RuleViolation.ToString()].Summary.Should()
            .Be(Resources.DefaultResponsesFilter_Example_RuleViolationError_Summary);
        _operation.Responses["400"].Content[HttpConstants.ContentTypes.JsonProblem]
            .Examples[ErrorCode.RuleViolation.ToString()].Value.Should().NotBeNull();
        _operation.Responses["400"].Description.Should().EndWith(StatusCode.BadRequest.Reason);
        _operation.Responses["401"].Description.Should().EndWith(StatusCode.Unauthorized.Reason);
        _operation.Responses["402"].Description.Should().EndWith(StatusCode.PaymentRequired.Reason);
        _operation.Responses["403"].Description.Should().EndWith(StatusCode.Forbidden.Reason);
        _operation.Responses["404"].Description.Should().EndWith(StatusCode.NotFound.Reason);
        _operation.Responses["409"].Description.Should().EndWith(StatusCode.Conflict.Reason);
        _operation.Responses["423"].Description.Should().EndWith(StatusCode.Locked.Reason);
        _operation.Responses["500"].Description.Should().EndWith(StatusCode.InternalServerError.Reason);
    }

    [Fact]
    public void WhenApplyAndPostVoidRequest_ThenSetsEmptySuccessResponses()
    {
        var request = (object _, TestPostRequestVoidResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2);
        _operation.Responses["200"].Description.Should().Be("OK");
        _operation.Responses["200"].Content.Should().BeNull();
    }

    [Fact]
    public void WhenApplyAndPostStreamRequest_ThenSetsEmptySuccessResponses()
    {
        var request = (object _, TestPostRequestStreamResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2);
        _operation.Responses["200"].Description.Should().Be("OK");
        _operation.Responses["200"].Content.Count.Should().Be(4);
        _operation.Responses["200"].Content[HttpConstants.ContentTypes.OctetStream].Schema.Type.Should().Be("string");
        _operation.Responses["200"].Content[HttpConstants.ContentTypes.ImageGif].Schema.Type.Should().Be("string");
        _operation.Responses["200"].Content[HttpConstants.ContentTypes.ImageJpeg].Schema.Type.Should().Be("string");
        _operation.Responses["200"].Content[HttpConstants.ContentTypes.ImagePng].Schema.Type.Should().Be("string");
    }

    [Fact]
    public void WhenApplyAndPostResponseRequest_ThenSetsContentOfSuccessResponse()
    {
        var request = (object _, TestPostRequestTypedResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);
        _schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(TestResponse), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "TestResponse" });

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2);
        _operation.Responses["200"].Description.Should().Be("OK");
        _operation.Responses["200"].Content.Count.Should().Be(2);
        _operation.Responses["200"].Content[HttpConstants.ContentTypes.Json].Schema.Type.Should().Be("TestResponse");
        _operation.Responses["200"].Content[HttpConstants.ContentTypes.Xml].Schema.Type.Should().Be("TestResponse");
    }

    [Fact]
    public void WhenApplyAndExistingAny20XResponseDescription_ThenSetsDescriptionOfSuccessResponse()
    {
        var request = (object _, TestPostRequestTypedResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);
        _schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(TestResponse), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "TestResponse" });
        _operation.Responses.Add("201", new OpenApiResponse { Description = "adescription" });

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2);
        _operation.Responses["200"].Description.Should().Be("OK: adescription");
    }

    [Fact]
    public void WhenApplyAndExistingAndMatching4XXResponsesDescription_ThenSetsDescriptionOfErrorResponse()
    {
        var request = (object _, TestPostRequestTypedResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);
        _schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(TestResponse), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "TestResponse" });
        _operation.Responses.Add("404", new OpenApiResponse { Description = "adescription" });

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2);
        _operation.Responses["200"].Description.Should().Be("OK");
        _operation.Responses["404"].Description.Should().Be("adescription");
    }

    [Fact]
    public void WhenApplyAndExistingAndNonDefault3XXResponsesDescription_ThenAddsErrorResponse()
    {
        var request = (object _, TestPostRequestTypedResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);
        _schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(TestResponse), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "TestResponse" });
        _operation.Responses.Add("302", new OpenApiResponse { Description = "adescription" });

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2 + 1);
        _operation.Responses["200"].Description.Should().Be("OK");
        _operation.Responses["302"].Description.Should().Be("adescription");
    }
    
    [Fact]
    public void WhenApplyAndExistingAndNonDefault4XXResponsesDescription_ThenAddsErrorResponse()
    {
        var request = (object _, TestPostRequestTypedResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);
        _schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(TestResponse), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "TestResponse" });
        _operation.Responses.Add("419", new OpenApiResponse { Description = "adescription" });

        _filter.Apply(_operation, _context);

        _operation.Responses.Count.Should().Be(HttpConstants.StatusCodes.SupportedErrorStatuses.Count + 2 + 1);
        _operation.Responses["200"].Description.Should().Be("OK");
        _operation.Responses["419"].Description.Should().Be("adescription");
    }
}

[UsedImplicitly]
public class TestPostRequestVoidResponse : IWebRequest
{
}

[UsedImplicitly]
public class TestPostRequestStreamResponse : IWebRequestStream
{
}

[UsedImplicitly]
public class TestPostRequestTypedResponse : WebRequest<TestPostRequestTypedResponse, TestResponse>
{
}

public class TestResponse : IWebResponse
{
    public required TestDto TestDto { get; set; }
}

public class TestDto
{
    public string? AProperty { get; set; }
}