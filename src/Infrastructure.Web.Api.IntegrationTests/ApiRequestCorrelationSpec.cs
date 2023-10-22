#if TESTINGONLY
using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Interfaces.Operations.TestingOnly;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[UsedImplicitly]
public class ApiRequestCorrelationSpec
{
    [Trait("Category", "Integration.Web")]
    public class GivenAnHttpClient : WebApiSpec<Program>
    {
        public GivenAnHttpClient(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenGetWithNoRequestId_ThenReturnsGeneratedResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpHeaders.RequestId).FirstOrDefault().Should()
                .NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenGetWithRequestId_ThenReturnsSameResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest(),
                message => { message.Headers.Add("Request-ID", "acorrelationid"); });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpHeaders.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }

        [Fact]
        public async Task WhenGetWithXRequestId_ThenReturnsSameResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest(),
                message => { message.Headers.Add("X-Request-ID", "acorrelationid"); });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpHeaders.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }

        [Fact]
        public async Task WhenGetWithXCorrelationId_ThenReturnsSameResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest(),
                message => { message.Headers.Add("X-Correlation-ID", "acorrelationid"); });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpHeaders.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }
    }

    [Trait("Category", "Integration.Web")]
    public class GivenAJsonClient : WebApiSpec<Program>
    {
        public GivenAJsonClient(WebApiSetup<Program> setup) : base(setup)
        {
        }

        [Fact]
        public async Task WhenGetWithNoRequestId_ThenReturnsGeneratedResponseHeader()
        {
            var result = await Api.GetAsync(new RequestCorrelationsTestingOnlyRequest());

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.RequestId.Should().NotBeNullOrEmpty();
        }
    }
}
#endif