#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
using ApiHost1;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Infrastructure.Api.Common.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class WebApiSpec : WebApiSpecSetup<Program>
{
    public WebApiSpec(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task WhenGetTestingOnlyUnvalidatedRequest_ThenReturns200()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenGetTestingOnlyUnvalidatedRequest_ThenReturnsJsonByDefault()
    {
        var result = await Api.GetFromJsonAsync<GetTestingOnlyResponse>("/testingonly/1/unvalidated");

        result?.Message.Should().Be("amessage1");
    }

    [Fact]
    public async Task WhenGetTestingOnlyValidatedRequestWithInvalidId_ThenReturnsValidationError()
    {
        var result = await Api.GetAsync("/testingonly/notanid/validated");

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await result.Content.ReadAsStringAsync();

        json.Should().Be(new[]
        {
            new
            {
                propertyName = "Id",
                errorMessage = "The Id was either invalid or missing",
                attemptedValue = "notanid",
                customState = (string)null!,
                severity = 0,
                errorCode = "RegularExpressionValidator",
                formattedMessagePlaceholderValues = new
                {
                    RegularExpression = "\\\\d{1,3}",
                    PropertyName = "Id",
                    PropertyValue = "notanid",
                    PropertyPath = "Id"
                }
            }
        }.ToJson(false, includeNulls: true));
    }
}
#endif