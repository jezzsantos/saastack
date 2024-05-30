#if TESTINGONLY
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Xunit;
using Resources = ApiHost1.Resources;

namespace Infrastructure.Web.Api.IntegrationTests;

[UsedImplicitly]
public class ValidationApiSpec
{
    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAnUnvalidatedRequest : WebApiSpec<ApiHost1.Program>
    {
        public GivenAnUnvalidatedRequest(WebApiSetup<ApiHost1.Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenGetRequest_ThenReturns200()
        {
            var result = await Api.GetAsync(new ValidationsUnvalidatedTestingOnlyRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Message.Should().Be("amessage");
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAValidatedGetRequest : WebApiSpec<ApiHost1.Program>
    {
        public GivenAValidatedGetRequest(WebApiSetup<ApiHost1.Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenGetRequestWithNullPathField_ThenReturnsMissingError()
        {
            var result = await Api.GetAsync(new ValidationsValidatedGetTestingOnlyRequest
            {
                Id = null!,
                RequiredField = null!
            });

            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Content.Error.Type.Should().BeNull();
            result.Content.Error.Title.Should().Be("Not Found");
            result.Content.Error.Status.Should().Be(404);
            result.Content.Error.Detail.Should().BeNull();
            result.Content.Error.Instance.Should().BeNull();
            result.Content.Error.Exception.Should().BeNull();
            result.Content.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetRequestWithNullRequiredField_ThenReturnsValidationError()
        {
            var result = await Api.GetAsync(new ValidationsValidatedGetTestingOnlyRequest
            {
                Id = "1234",
                RequiredField = null!
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Type.Should().Be("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5");
            result.Content.Error.Title.Should().Be("Bad Request");
            result.Content.Error.Status.Should().Be(400);
            result.Content.Error.Detail.Should().Be("'Required Field' must not be empty.");
            result.Content.Error.Instance.Should()
                .Be(@"https://localhost/testingonly/validations/validated/1234");
            result.Content.Error.Exception.Should().BeNull();
            result.Content.Error.Errors.Should().BeEquivalentTo(new ValidatorProblem[]
            {
                new()
                {
                    Rule = "NotEmptyValidator", Reason = "'Required Field' must not be empty.", Value = null
                }
            });
        }

        [Fact]
        public async Task WhenGetRequestWithInvalidFields_ThenReturnsValidationError()
        {
            var result = await Api.GetAsync(new ValidationsValidatedGetTestingOnlyRequest
            {
                Id = "1234",
                RequiredField = "invalid",
                OptionalField = "invalid"
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Type.Should().Be("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5");
            result.Content.Error.Title.Should().Be("Bad Request");
            result.Content.Error.Status.Should().Be(400);
            result.Content.Error.Detail.Should()
                .Be(Resources.GetTestingOnlyValidatedRequestValidator_InvalidRequiredField);
            result.Content.Error.Instance.Should()
                .Be(
                    "https://localhost/testingonly/validations/validated/1234?optionalfield=invalid&requiredfield=invalid");
            result.Content.Error.Exception.Should().BeNull();
            result.Content.Error.Errors!.Length.Should().Be(2);
            result.Content.Error.Errors[0].Rule.Should().Be("RegularExpressionValidator");
            result.Content.Error.Errors[0].Reason.Should()
                .Be(Resources.GetTestingOnlyValidatedRequestValidator_InvalidRequiredField);
            result.Content.Error.Errors[0].Value.As<JsonElement>().GetString().Should().Be("invalid");
            result.Content.Error.Errors[1].Rule.Should().Be("RegularExpressionValidator");
            result.Content.Error.Errors[1].Reason.Should()
                .Be(Resources.GetTestingOnlyValidatedRequestValidator_InvalidOptionalField);
            result.Content.Error.Errors[1].Value.As<JsonElement>().GetString().Should().Be("invalid");
        }

        [Fact]
        public async Task WhenGetRequestWithOnlyRequiredFields_ThenReturnsResponse()
        {
            var result =
                await Api.GetAsync(new ValidationsValidatedGetTestingOnlyRequest
                {
                    Id = "1234",
                    RequiredField = "123"
                });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Message.Should().Be("amessage123");
        }

        [Fact]
        public async Task WhenGetRequestWithAllFields_ThenReturnsResponse()
        {
            var result =
                await Api.GetAsync(new ValidationsValidatedGetTestingOnlyRequest
                {
                    Id = "1234",
                    RequiredField = "123",
                    OptionalField = "789"
                });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Value.Message.Should().Be("amessage123");
        }
    }

    [Trait("Category", "Integration.API")]
    [Collection("API")]
    public class GivenAValidatedPostRequest : WebApiSpec<ApiHost1.Program>
    {
        public GivenAValidatedPostRequest(WebApiSetup<ApiHost1.Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenGetRequestWithNullPathField_ThenReturnsMissingError()
        {
            var result = await Api.PostAsync(new ValidationsValidatedPostTestingOnlyRequest
            {
                Id = null!,
                RequiredField = null!
            });

            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Content.Error.Type.Should().BeNull();
            result.Content.Error.Title.Should().Be("Not Found");
            result.Content.Error.Status.Should().Be(404);
            result.Content.Error.Detail.Should().BeNull();
            result.Content.Error.Instance.Should().BeNull();
            result.Content.Error.Exception.Should().BeNull();
            result.Content.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task WhenPostRequestWithNullRequiredField_ThenReturnsValidationError()
        {
            var result = await Api.PostAsync(new ValidationsValidatedPostTestingOnlyRequest
            {
                Id = "1234",
                RequiredField = null!
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Type.Should().Be("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5");
            result.Content.Error.Title.Should().Be("Bad Request");
            result.Content.Error.Status.Should().Be(400);
            result.Content.Error.Detail.Should().Be("'Required Field' must not be empty.");
            result.Content.Error.Instance.Should()
                .Be(@"https://localhost/testingonly/validations/validated/1234");
            result.Content.Error.Exception.Should().BeNull();
            result.Content.Error.Errors.Should().BeEquivalentTo(new ValidatorProblem[]
            {
                new()
                {
                    Rule = "NotEmptyValidator", Reason = "'Required Field' must not be empty.", Value = null
                }
            });
        }

        [Fact]
        public async Task WhenPostRequestWithInvalidFields_ThenReturnsValidationError()
        {
            var result = await Api.PostAsync(new ValidationsValidatedPostTestingOnlyRequest
            {
                Id = "1234",
                RequiredField = "invalid",
                OptionalField = "invalid"
            });

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Content.Error.Type.Should().Be("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5");
            result.Content.Error.Title.Should().Be("Bad Request");
            result.Content.Error.Status.Should().Be(400);
            result.Content.Error.Detail.Should()
                .Be(Resources.GetTestingOnlyValidatedRequestValidator_InvalidRequiredField);
            result.Content.Error.Instance.Should()
                .Be("https://localhost/testingonly/validations/validated/1234");
            result.Content.Error.Exception.Should().BeNull();
            result.Content.Error.Errors!.Length.Should().Be(2);
            result.Content.Error.Errors[0].Rule.Should().Be("RegularExpressionValidator");
            result.Content.Error.Errors[0].Reason.Should()
                .Be(Resources.GetTestingOnlyValidatedRequestValidator_InvalidRequiredField);
            result.Content.Error.Errors[0].Value.As<JsonElement>().GetString().Should().Be("invalid");
            result.Content.Error.Errors[1].Rule.Should().Be("RegularExpressionValidator");
            result.Content.Error.Errors[1].Reason.Should()
                .Be(Resources.GetTestingOnlyValidatedRequestValidator_InvalidOptionalField);
            result.Content.Error.Errors[1].Value.As<JsonElement>().GetString().Should().Be("invalid");
        }

        [Fact]
        public async Task WhenPostRequestWithOnlyRequiredFields_ThenReturnsResponse()
        {
            var result =
                await Api.PostAsync(new ValidationsValidatedPostTestingOnlyRequest
                {
                    Id = "1234",
                    RequiredField = "123"
                });

            result.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Content.Value.Message.Should().Be("amessage123");
        }

        [Fact]
        public async Task WhenPostRequestWithAllFields_ThenReturnsResponse()
        {
            var result =
                await Api.PostAsync(new ValidationsValidatedPostTestingOnlyRequest
                {
                    Id = "1234",
                    RequiredField = "123",
                    OptionalField = "789"
                });

            result.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Content.Value.Message.Should().Be("amessage123");
        }
    }
}
#endif