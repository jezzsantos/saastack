#if TESTINGONLY
using ApiHost1;
using Common.Extensions;
using FluentAssertions;
using HtmlAgilityPack;
using Infrastructure.Hosting.Common;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Hosting.Common.Auth;
using Infrastructure.Web.Hosting.Common.Documentation;
using Infrastructure.Web.Interfaces;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Xunit;
using Resources = Infrastructure.Web.Interfaces.Resources;

namespace Infrastructure.Web.Api.IntegrationTests;

[UsedImplicitly]
public class ApiDocSpec
{
    private static void VerifyGeneralErrorResponses(OpenApiResponses responses, string? conflictMessage = null)
    {
        responses["400"].Description.Should()
            .Be($"{StatusCode.BadRequest.Title}: {Resources.HttpConstants_StatusCodes_Reason_BadRequest}");
        responses["401"].Description.Should()
            .Be($"{StatusCode.Unauthorized.Title}: {Resources.HttpConstants_StatusCodes_Reason_Unauthorized}");
        responses["402"].Description.Should()
            .Be($"{StatusCode.PaymentRequired.Title}: {Resources.HttpConstants_StatusCodes_Reason_PaymentRequired}");
        responses["403"].Description.Should()
            .Be($"{StatusCode.Forbidden.Title}: {Resources.HttpConstants_StatusCodes_Reason_Forbidden}");
        responses["404"].Description.Should()
            .Be($"{StatusCode.NotFound.Title}: {Resources.HttpConstants_StatusCodes_Reason_NotFound}");
        responses["405"].Description.Should()
            .Be($"{StatusCode.MethodNotAllowed.Title}: {Resources.HttpConstants_StatusCodes_Reason_MethodNotAllowed}");
        responses["409"].Description.Should()
            .Be(conflictMessage.HasValue()
                ? conflictMessage
                : $"{StatusCode.Conflict.Title}: {Resources.HttpConstants_StatusCodes_Reason_Conflict}");
        responses["423"].Description.Should()
            .Be($"{StatusCode.Locked.Title}: {Resources.HttpConstants_StatusCodes_Reason_Locked}");
        responses["500"].Description.Should()
            .Be(
                $"{StatusCode.InternalServerError.Title}: {Resources.HttpConstants_StatusCodes_Reason_InternalServerError}");
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnyRequest : WebApiSpec<Program>
    {
        private static OpenApiDocument? _document;

        public GivenAnyRequest(WebApiSetup<Program> setup) : base(setup)
        {
            if (_document.NotExists())
            {
                var result = HttpApi.GetAsync(WebConstants.SwaggerEndpointFormat.Format("v1"))
                    .GetAwaiter().GetResult();

                var readAsStreamAsync = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                _document = new OpenApiStreamReader().Read(readAsStreamAsync, out _);
            }
        }

        [Fact]
        public async Task WhenGetSwaggerUI_ThenDisplayed()
        {
            var result = await HttpApi.GetAsync("/index.html");

            var content = await result.Content.ReadAsStringAsync();
            content.Should().Contain("<html");

            var doc = new HtmlDocument();
            doc.Load(await result.Content.ReadAsStreamAsync());

            var swaggerPage = doc.DocumentNode
                .SelectSingleNode("//div[@id='swagger-ui']");

            swaggerPage.Should().NotBeNull();

            var title = doc.DocumentNode.SelectSingleNode("//title");

            title.Should().NotBeNull();
            title.InnerText.Should().Be(HostOptions.BackEndAncillaryApiHost.HostName);
        }

        [Fact]
        public void WhenFetchOpenApi_ThenHasXmlSummary()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Get];
            operation.Description.Should().Be($"(request type: {nameof(OpenApiGetTestingOnlyRequest)})");
            operation.OperationId.Should().Be("OpenApiGetTestingOnly");
            operation.Summary.Should()
                .Be("Tests OpenAPI swagger for GET requests This includes multiple lines explaining things");
        }

        [Fact]
        public void WhenFetchOpenApi_ThenHasXmlSpecialResponses()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Post];
            operation.Description.Should().Be($"(request type: {nameof(OpenApiPostTestingOnlyRequest)})");
            operation.OperationId.Should().Be("OpenApiPostTestingOnly");
            operation.Summary.Should().Be("Tests OpenAPI swagger for POST requests");
            operation.Responses["409"].Description.Should()
                .Be("a custom conflict response which spills over to the next line");
            operation.Responses["419"].Description.Should().Be("a special response");
        }

        [Fact]
        public void WhenFetchOpenApi_ThenHasNullableResponseFields()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Get];
            operation.Description.Should().Be($"(request type: {nameof(OpenApiGetTestingOnlyRequest)})");
            operation.OperationId.Should().Be("OpenApiGetTestingOnly");

            var responseType = _document.Components.Schemas[nameof(OpenApiTestingOnlyResponse)];
            responseType.Required.Count.Should().Be(5);
            responseType.Required.Should().ContainInOrder("anAnnotatedRequiredField", "anInitializedField",
                "aRequiredField", "aValueTypeField", "message");
            responseType.Properties["anAnnotatedRequiredField"].Nullable.Should().BeFalse();
            responseType.Properties["anInitializedField"].Nullable.Should().BeFalse();
            responseType.Properties["aNullableField"].Nullable.Should().BeFalse();
            responseType.Properties["aRequiredField"].Nullable.Should().BeFalse();
            responseType.Properties["aValueTypeField"].Nullable.Should().BeFalse();
            responseType.Properties["aNullableValueTypeField"].Nullable.Should().BeFalse();
            responseType.Properties["message"].Nullable.Should().BeFalse();
            responseType.Properties["anNullableObject"].Nullable.Should().BeFalse();

            var nestedType = _document.Components.Schemas[nameof(OpenApiResponseObject)];
            nestedType.Required.Count.Should().Be(4);
            nestedType.Required.Should().ContainInOrder("anAnnotatedRequiredField", "anInitializedField",
                "aRequiredField", "aValueTypeField");
            nestedType.Properties["anAnnotatedRequiredField"].Nullable.Should().BeFalse();
            nestedType.Properties["anInitializedField"].Nullable.Should().BeFalse();
            nestedType.Properties["aNullableField"].Nullable.Should().BeFalse();
            nestedType.Properties["aRequiredField"].Nullable.Should().BeFalse();
            nestedType.Properties["aValueTypeField"].Nullable.Should().BeFalse();
            nestedType.Properties["aNullableValueTypeField"].Nullable.Should().BeFalse();
        }

        [Fact]
        public void WhenFetchOpenApiForNonSecuredEndpoint_ThenAnonymousAuthorizationHasNoSecurityScheme()
        {
            var operation = _document!.Paths["/testingonly/security/none"].Operations[OperationType.Get];
            operation.Description.Should().Be($"(request type: {nameof(GetInsecureTestingOnlyRequest)})");
            operation.OperationId.Should().Be("GetInsecureTestingOnly");
            operation.Security.Should().HaveCount(0);
        }

        [Fact]
        public void WhenFetchOpenApiForTokenSecuredEndpoint_ThenTokenAuthorizationHasSecurityScheme()
        {
            var operation = _document!.Paths["/testingonly/authn/token/get"].Operations[OperationType.Get];
            operation.Description.Should()
                .Be($"(request type: {nameof(GetCallerWithTokenOrAPIKeyTestingOnlyRequest)})");
            operation.OperationId.Should().Be("GetCallerWithTokenOrAPIKeyTestingOnly");
            operation.Security.Should().HaveCount(2);
            operation.Security[0].Values.Count.Should().Be(1);
            operation.Security[0].First().Key.Reference.Id.Should().Be(JwtBearerDefaults.AuthenticationScheme);
            operation.Security[0].First().Key.Type.Should().Be(SecuritySchemeType.ApiKey);
            operation.Security[1].Values.Count.Should().Be(1);
            operation.Security[1].First().Key.Reference.Id.Should()
                .Be(APIKeyAuthenticationHandler.AuthenticationScheme);
            operation.Security[1].First().Key.Type.Should().Be(SecuritySchemeType.ApiKey);
        }

        [Fact]
        public void WhenFetchOpenApiForSecureHMACEndpoint_ThenHMACAuthorizationHasSecurityScheme()
        {
            var operation = _document!.Paths["/testingonly/authn/hmac/get"].Operations[OperationType.Get];
            operation.Description.Should().Be($"(request type: {nameof(GetCallerWithHMACTestingOnlyRequest)})");
            operation.OperationId.Should().Be("GetCallerWithHMACTestingOnly");
            operation.Security.Should().HaveCount(1);
            operation.Security[0].Values.Count.Should().Be(1);
            operation.Security[0].First().Key.Reference.Id.Should().Be(HMACAuthenticationHandler.AuthenticationScheme);
            operation.Security[0].First().Key.Type.Should().Be(SecuritySchemeType.ApiKey);
        }

        [Fact]
        public void WhenFetchOpenApiForSecurePrivateInterHostEndpoint_ThenShouldNotExist()
        {
            var operation = _document!.Paths.ContainsKey("/testingonly/authn/interhost/get");
            operation.Should().BeFalse();
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAGetRequest : WebApiSpec<Program>
    {
        private static OpenApiDocument? _document;

        public GivenAGetRequest(WebApiSetup<Program> setup) : base(setup)
        {
            if (_document.NotExists())
            {
                var result = HttpApi.GetAsync(WebConstants.SwaggerEndpointFormat.Format("v1"))
                    .GetAwaiter().GetResult();

                var readAsStreamAsync = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                _document = new OpenApiStreamReader().Read(readAsStreamAsync, out _);
            }
        }

        [Fact]
        public void WhenFetchOpenApi_ThenParametersHaveDescriptions()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Get];
            operation.Description.Should().Be($"(request type: {nameof(OpenApiGetTestingOnlyRequest)})");
            operation.OperationId.Should().Be("OpenApiGetTestingOnly");
            operation.Parameters.Should().HaveCount(3);
            operation.Parameters[0].Name.Should().Be("Id");
            operation.Parameters[0].Description.Should().Be("anid");
            operation.Parameters[0].Required.Should().BeTrue();
            operation.Parameters[1].Name.Should().Be("OptionalField");
            operation.Parameters[1].Description.Should().Be("anoptionalfield");
            operation.Parameters[1].Required.Should().BeFalse();
            operation.Parameters[2].Name.Should().Be("RequiredField");
            operation.Parameters[2].Description.Should().Be("arequiredfield");
            operation.Parameters[2].Required.Should().BeTrue();
        }

        [Fact]
        public void WhenFetchOpenApi_ThenResponseHas200Response()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Get];
            operation.Responses["200"].Description.Should().Be("OK");
            operation.Responses["200"].Content.Count.Should().Be(2);
            operation.Responses["200"].Content[HttpConstants.ContentTypes.Json].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
            operation.Responses["200"].Content[HttpConstants.ContentTypes.Xml].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
        }

        [Fact]
        public void WhenFetchOpenApi_ThenResponseHasGeneralErrorResponses()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Get];
            operation.Responses.Count.Should().Be(10);
            VerifyGeneralErrorResponses(operation.Responses);
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAPostRequest : WebApiSpec<Program>
    {
        private static OpenApiDocument? _document;

        public GivenAPostRequest(WebApiSetup<Program> setup) : base(setup)
        {
            if (_document.NotExists())
            {
                var result = HttpApi.GetAsync(WebConstants.SwaggerEndpointFormat.Format("v1"))
                    .GetAwaiter().GetResult();

                var readAsStreamAsync = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                _document = new OpenApiStreamReader().Read(readAsStreamAsync, out _);
            }
        }

        [Fact]
        public void WhenFetchOpenApi_ThenFieldsHaveDescriptions()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Post];
            operation.Description.Should().Be($"(request type: {nameof(OpenApiPostTestingOnlyRequest)})");
            operation.OperationId.Should().Be("OpenApiPostTestingOnly");
            var schema = _document.Components.Schemas[nameof(OpenApiPostTestingOnlyRequest)];
            schema.Description.Should().BeNull();
            schema.Required.Should().BeEquivalentTo("id", "requiredField");
            schema.Properties.Should().HaveCount(2);
            schema.Properties.Should().NotContainKey("id");
            schema.Properties["optionalField"].Description.Should().Be("anoptionalfield");
            schema.Properties["requiredField"].Description.Should().Be("arequiredfield");
        }

        [Fact]
        public void WhenFetchOpenApi_ThenResponseHas201Response()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Post];
            operation.Responses["201"].Description.Should().Be("Created");
            operation.Responses["201"].Content.Count.Should().Be(2);
            operation.Responses["201"].Content[HttpConstants.ContentTypes.Json].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
            operation.Responses["201"].Content[HttpConstants.ContentTypes.Xml].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
        }

        [Fact]
        public void WhenFetchOpenApi_ThenResponseHasGeneralErrorResponses()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Post];
            operation.Responses.Count.Should().Be(11);
            VerifyGeneralErrorResponses(operation.Responses,
                "a custom conflict response which spills over to the next line");
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAPutRequest : WebApiSpec<Program>
    {
        private static OpenApiDocument? _document;

        public GivenAPutRequest(WebApiSetup<Program> setup) : base(setup)
        {
            if (_document.NotExists())
            {
                var result = HttpApi.GetAsync(WebConstants.SwaggerEndpointFormat.Format("v1"))
                    .GetAwaiter().GetResult();

                var readAsStreamAsync = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                _document = new OpenApiStreamReader().Read(readAsStreamAsync, out _);
            }
        }

        [Fact]
        public void WhenFetchOpenApi_ThenFieldsHaveDescriptions()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Put];
            operation.Description.Should().Be($"(request type: {nameof(OpenApiPutTestingOnlyRequest)})");
            operation.OperationId.Should().Be("OpenApiPutTestingOnly (Put)");
            var schema = _document.Components.Schemas[nameof(OpenApiPutTestingOnlyRequest)];
            schema.Description.Should().BeNull();
            schema.Required.Should().BeEquivalentTo("id", "requiredField");
            schema.Properties.Should().HaveCount(2);
            schema.Properties.Should().NotContainKey("id");
            schema.Properties["optionalField"].Description.Should().Be("anoptionalfield");
            schema.Properties["requiredField"].Description.Should().Be("arequiredfield");
        }

        [Fact]
        public void WhenFetchOpenApi_ThenResponseHas201Response()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Put];
            operation.Responses["202"].Description.Should().Be("Accepted");
            operation.Responses["202"].Content.Count.Should().Be(2);
            operation.Responses["202"].Content[HttpConstants.ContentTypes.Json].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
            operation.Responses["202"].Content[HttpConstants.ContentTypes.Xml].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
        }

        [Fact]
        public void WhenFetchOpenApi_ThenResponseHasGeneralErrorResponses()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}"].Operations[OperationType.Put];
            operation.Responses.Count.Should().Be(11);
            VerifyGeneralErrorResponses(operation.Responses, "a custom conflict response");
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAPostMultiPartFormDataRequest : WebApiSpec<Program>
    {
        private static OpenApiDocument? _document;

        public GivenAPostMultiPartFormDataRequest(WebApiSetup<Program> setup) : base(setup)
        {
            if (_document.NotExists())
            {
                var result = HttpApi.GetAsync(WebConstants.SwaggerEndpointFormat.Format("v1"))
                    .GetAwaiter().GetResult();

                var readAsStreamAsync = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                _document = new OpenApiStreamReader().Read(readAsStreamAsync, out _);
            }
        }

        [Fact]
        public void WhenFetchOpenApiForMultiPartFormData_ThenFieldsHaveDescriptions()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}/form-data"].Operations[OperationType.Post];
#if TESTINGONLY
            operation.Description.Should()
                .Be($"(request type: {nameof(OpenApiPostMultiPartFormDataTestingOnlyRequest)})");
#endif
            operation.OperationId.Should().Be("OpenApiPostMultiPartFormDataTestingOnly");
            operation.Parameters.Count.Should().Be(1);
            operation.Parameters[0].Name.Should().Be("Id");
            operation.Parameters[0].In.Should().Be(ParameterLocation.Path);
            var requestBody = operation.RequestBody;
            requestBody.Content.Count.Should().Be(1);
            requestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Required.Should()
                .BeEquivalentTo(DefaultBodyFilter.FormFilesFieldName, "requiredField");
            var properties = requestBody.Content[HttpConstants.ContentTypes.MultiPartFormData].Schema.Properties;
            properties.Should().HaveCount(3);
            properties[DefaultBodyFilter.FormFilesFieldName].Items.Format.Should().Be("binary");
            properties["optionalField"].Type.Should().Be("string");
            properties["requiredField"].Type.Should().Be("string");
        }

        [Fact]
        public void WhenFetchOpenApiForMultiPartFormData_ThenResponseHas201Response()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}/form-data"].Operations[OperationType.Post];
            operation.Responses["201"].Description.Should().Be("Created");
            operation.Responses["201"].Content.Count.Should().Be(2);
            operation.Responses["201"].Content[HttpConstants.ContentTypes.Json].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
            operation.Responses["201"].Content[HttpConstants.ContentTypes.Xml].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
        }

        [Fact]
        public void WhenFetchOpenApiForMultiPartFormData_ThenResponseHasGeneralErrorResponses()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}/form-data"].Operations[OperationType.Post];
            operation.Responses.Count.Should().Be(10);
            VerifyGeneralErrorResponses(operation.Responses);
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAPostFormUrlEncodingRequest : WebApiSpec<Program>
    {
        private static OpenApiDocument? _document;

        public GivenAPostFormUrlEncodingRequest(WebApiSetup<Program> setup) : base(setup)
        {
            if (_document.NotExists())
            {
                var result = HttpApi.GetAsync(WebConstants.SwaggerEndpointFormat.Format("v1"))
                    .GetAwaiter().GetResult();

                var readAsStreamAsync = result.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                _document = new OpenApiStreamReader().Read(readAsStreamAsync, out _);
            }
        }

        [Fact]
        public void WhenFetchOpenApiForFormUrlEncoded_ThenFieldsHaveDescriptions()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}/urlencoded"].Operations[OperationType.Post];
#if TESTINGONLY
            operation.Description.Should().Be($"(request type: {nameof(OpenApiPostFormUrlEncodedTestingOnlyRequest)})");
#endif
            operation.OperationId.Should().Be("OpenApiPostFormUrlEncodedTestingOnly");
            operation.Parameters.Count.Should().Be(1);
            operation.Parameters[0].Name.Should().Be("Id");
            operation.Parameters[0].In.Should().Be(ParameterLocation.Path);
            var requestBody = operation.RequestBody;
            requestBody.Content.Count.Should().Be(1);
            requestBody.Content[HttpConstants.ContentTypes.FormUrlEncoded].Schema.Required.Should()
                .BeEquivalentTo("requiredField");
            var properties = requestBody.Content[HttpConstants.ContentTypes.FormUrlEncoded].Schema.Properties;
            properties.Should().HaveCount(2);
            properties["optionalField"].Type.Should().Be("string");
            properties["requiredField"].Type.Should().Be("string");
        }

        [Fact]
        public void WhenFetchOpenApiForFormUrlEncoded_ThenResponseHas201Response()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}/urlencoded"].Operations[OperationType.Post];
            operation.Responses["201"].Description.Should().Be("Created");
            operation.Responses["201"].Content.Count.Should().Be(2);
            operation.Responses["201"].Content[HttpConstants.ContentTypes.Json].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
            operation.Responses["201"].Content[HttpConstants.ContentTypes.Xml].Schema.Reference.ReferenceV3.Should()
                .Be($"#/components/schemas/{nameof(OpenApiTestingOnlyResponse)}");
        }

        [Fact]
        public void WhenFetchOpenApiForFormUrlEncoded_ThenResponseHasGeneralErrorResponses()
        {
            var operation = _document!.Paths["/testingonly/openapi/{Id}/urlencoded"].Operations[OperationType.Post];
            operation.Responses.Count.Should().Be(10);
            VerifyGeneralErrorResponses(operation.Responses);
        }
    }
}
#endif