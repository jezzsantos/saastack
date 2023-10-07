#if TESTINGONLY
using System.Net;
using ApiHost1;
using FluentAssertions;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.WebApi.Common.IntegrationTests;

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
            var result = await HttpApi.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("testingonly/correlations/get", UriKind.Relative)
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpHeaders.RequestId).FirstOrDefault().Should()
                .NotBeNullOrEmpty();
        }

        [Fact]
        public async Task WhenGetWithRequestId_ThenReturnsSameResponseHeader()
        {
            var result = await HttpApi.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("testingonly/correlations/get", UriKind.Relative),
                Headers = { { "Request-ID", "acorrelationid" } }
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpHeaders.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }

        [Fact]
        public async Task WhenGetWithXRequestId_ThenReturnsSameResponseHeader()
        {
            var result = await HttpApi.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("testingonly/correlations/get", UriKind.Relative),
                Headers = { { "X-Request-ID", "acorrelationid" } }
            });

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Headers.GetValues(HttpHeaders.RequestId).FirstOrDefault().Should()
                .Be("acorrelationid");
        }

        [Fact]
        public async Task WhenGetWithXCorrelationId_ThenReturnsSameResponseHeader()
        {
            var result = await HttpApi.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("testingonly/correlations/get", UriKind.Relative),
                Headers = { { "X-Correlation-ID", "acorrelationid" } }
            });

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
            var result = await Api.GetAsync("testingonly/correlations/get");

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.RequestId.Should().NotBeNullOrEmpty();
        }
    }
}
#endif