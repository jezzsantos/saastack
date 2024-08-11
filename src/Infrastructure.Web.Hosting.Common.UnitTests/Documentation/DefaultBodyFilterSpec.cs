using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Documentation;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Documentation;

[Trait("Category", "Unit")]
public class DefaultBodyFilterSpec
{
    private readonly ApiDescription _apiDescription;
    private readonly OperationFilterContext _context;
    private readonly DefaultBodyFilter _filter;
    private readonly Mock<MethodInfo> _methodInfo;
    private readonly OpenApiOperation _operation;
    private readonly ApiParameterDescription _parameterDescription;

    public DefaultBodyFilterSpec()
    {
        _operation = new OpenApiOperation
        {
            OperationId = "anoperationid"
        };
        var schemaGenerator = new Mock<ISchemaGenerator>();
        schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(string), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "string" });
        schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(DateTime), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "date" });
        schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(int), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema { Type = "int" });
        schemaGenerator.Setup(sg => sg.GenerateSchema(typeof(TestPostRequestVoidResponse), It.IsAny<SchemaRepository>(),
                It.IsAny<MemberInfo>(), It.IsAny<ParameterInfo>(), It.IsAny<ApiParameterRouteInfo>()))
            .Returns(new OpenApiSchema
            {
                Reference = new OpenApiReference
                {
                    Id = "areferenceid"
                }
            });

        _methodInfo = new Mock<MethodInfo>();

        _parameterDescription = new ApiParameterDescription();
        _apiDescription = new ApiDescription
        {
            ParameterDescriptions =
            {
                _parameterDescription
            },
            HttpMethod = HttpMethod.Post.Method
        };
        _context = new OperationFilterContext(_apiDescription, schemaGenerator.Object, new SchemaRepository(),
            _methodInfo.Object);
        _filter = new DefaultBodyFilter();
    }

    [Fact]
    public void WhenApplyAndIsNotWebRequest_ThenDoesNothing()
    {
        _filter.Apply(_operation, _context);

        _operation.RequestBody.Should().BeNull();
    }

    [Fact]
    public void WhenApplyAndHasNoBodyRequest_ThenDoesNothing()
    {
        _parameterDescription.Type = typeof(TestGetRequestTypedResponse);
        _apiDescription.HttpMethod = HttpMethod.Get.Method;

        _filter.Apply(_operation, _context);

        _operation.RequestBody.Should().BeNull();
    }

    [Fact]
    public void WhenApplyAndJsonBody_ThenSetsRequestBody()
    {
        var request = (object _, TestPostRequestVoidResponse req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);

        _filter.Apply(_operation, _context);

        _operation.RequestBody.Content.Count.Should().Be(1);
        _operation.RequestBody.Content[HttpConstants.ContentTypes.Json].Schema.Reference.Id.Should().Be("areferenceid");
        _operation.RequestBody.Content[HttpConstants.ContentTypes.Json].Schema.Reference.Type.Should()
            .Be(ReferenceType.Schema);
    }

    [Fact]
    public void WhenApplyAndMultiPartFormBody_ThenSetsRequestBodyAndIgnoresRouteProperties()
    {
        var request = (object _, TestMultipartFormRequest req) => { };
        _methodInfo.Setup(mi => mi.GetParameters())
            .Returns(request.GetMethodInfo().GetParameters);

        _filter.Apply(_operation, _context);

        _operation.RequestBody.Content.Count.Should().Be(1);
        _operation.RequestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Type.Should().Be("object");
        var properties = _operation.RequestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Properties;
        properties.Count.Should().Be(4);
        properties[DefaultBodyFilter.FormFilesFieldName].Type.Should().Be("array");
        properties[nameof(TestMultipartFormRequest.AStringProperty).ToCamelCase()].Type.Should().Be("string");
        properties[nameof(TestMultipartFormRequest.ADateProperty).ToCamelCase()].Type.Should().Be("date");
        properties[nameof(TestMultipartFormRequest.ANumberProperty).ToCamelCase()].Type.Should().Be("int");
        var required = _operation.RequestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Required
            .ToArray();
        required.Length.Should().Be(3);
        required[0].Should().Be(DefaultBodyFilter.FormFilesFieldName);
        required[1].Should().Be(nameof(TestMultipartFormRequest.ADateProperty).ToCamelCase());
        required[2].Should().Be(nameof(TestMultipartFormRequest.ANumberProperty).ToCamelCase());
    }
}

public class TestGetRequestTypedResponse : WebRequest<TestGetRequestTypedResponse, TestResponse>
{
}

[Route("/aroute/{AnId}", OperationMethod.Post)]
public class TestMultipartFormRequest : IWebRequest, IHasMultipartForm
{
    [Required] public DateTime ADateProperty { get; set; }

    public string? AnId { get; set; }

    [Required] public int ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }
}