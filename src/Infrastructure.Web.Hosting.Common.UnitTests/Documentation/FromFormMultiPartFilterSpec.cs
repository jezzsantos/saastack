using System.Reflection;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Documentation;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Moq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Documentation;

[Trait("Category", "Unit")]
public class FromFormMultiPartFilterSpec
{
    private readonly OperationFilterContext _context;
    private readonly FromFormMultiPartFilter _filter;
    private readonly OpenApiOperation _operation;
    private readonly ApiParameterDescription _parameterDescription;

    public FromFormMultiPartFilterSpec()
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

        Mock<MethodInfo> methodInfo = new();
        _parameterDescription = new ApiParameterDescription();
        var apiDescription = new ApiDescription
        {
            ParameterDescriptions =
            {
                _parameterDescription
            },
            HttpMethod = HttpMethod.Post.Method
        };
        _context = new OperationFilterContext(apiDescription, schemaGenerator.Object, new SchemaRepository(),
            methodInfo.Object);
        _filter = new FromFormMultiPartFilter();
    }

    [Fact]
    public void WhenApplyAndIsNotWebRequest_ThenDoesNothing()
    {
        _filter.Apply(_operation, _context);

        _operation.RequestBody.Should().BeNull();
    }

    [Fact]
    public void WhenApplyAndIsNotFromFormRequest_ThenDoesNothing()
    {
        _parameterDescription.Type = typeof(TestPostRequestVoidResponse);
        _parameterDescription.Source = BindingSource.Body;

        _filter.Apply(_operation, _context);

        _operation.RequestBody.Should().BeNull();
    }

    [Fact]
    public void WhenApplyAndIsFromFormRequest_ThenSetsRequestBody()
    {
        _parameterDescription.Type = typeof(TestMultipartFormRequest);
        _parameterDescription.Source = BindingSource.FormFile;

        _filter.Apply(_operation, _context);

        _operation.RequestBody.Content.Count.Should().Be(1);
        _operation.RequestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Type.Should().Be("object");
        var properties = _operation.RequestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Properties;
        properties.Count.Should().Be(4);
        properties[FromFormMultiPartFilter.FormFilesFieldName].Type.Should().Be("array");
        properties[nameof(TestMultipartFormRequest.AStringProperty)].Type.Should().Be("string");
        properties[nameof(TestMultipartFormRequest.ADateProperty)].Type.Should().Be("date");
        properties[nameof(TestMultipartFormRequest.ANumberProperty)].Type.Should().Be("int");
        var required = _operation.RequestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Required
            .ToArray();
        required.Length.Should().Be(3);
        required[0].Should().Be(FromFormMultiPartFilter.FormFilesFieldName);
        required[1].Should().Be(nameof(TestMultipartFormRequest.ADateProperty));
        required[2].Should().Be(nameof(TestMultipartFormRequest.ANumberProperty));
    }
}

public class TestMultipartFormRequest : IWebRequest, IHasMultipartForm
{
    public required DateTime ADateProperty { get; set; }

    public required int ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }
}