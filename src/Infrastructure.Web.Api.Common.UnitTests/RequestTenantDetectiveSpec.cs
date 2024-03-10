using System.Text;
using Common;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class RequestTenantDetectiveSpec
{
    [Trait("Category", "Unit")]
    public class GivenAnUntenantedRequestDto
    {
        private readonly RequestTenantDetective _detective;

        public GivenAnUntenantedRequestDto()
        {
            _detective = new RequestTenantDetective();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndNoRequestType_ThenReturnsNoTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, Optional<Type>.None, CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().BeNone();
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncButNoHeaderQueryOrBody_ThenReturnsNoTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Get
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().BeNone();
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInHeader_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Append(HttpHeaders.Tenant, "atenantid");

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInQueryString_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(ITenantedRequest.OrganizationId), "atenantid" }
                    }),
                    Method = HttpMethods.Get
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInJsonBody_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpContentTypes.Json,
                    Body = new MemoryStream(Encoding.UTF8.GetBytes(new
                    {
                        OrganizationId = "atenantid"
                    }.ToJson()!))
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncAndTenantIdInFormUrlEncodedBody_ThenReturnsTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request =
                {
                    Method = HttpMethods.Post,
                    ContentType = HttpContentTypes.FormUrlEncoded,
                    Body = await new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                    {
                        new(nameof(ITenantedRequest.OrganizationId), "atenantid")
                    }).ReadAsStreamAsync()
                }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestUnTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().Be("atenantid");
            result.Value.ShouldHaveTenantId.Should().BeFalse();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenATenantedRequestDto
    {
        private readonly RequestTenantDetective _detective;

        public GivenATenantedRequestDto()
        {
            _detective = new RequestTenantDetective();
        }

        [Fact]
        public async Task WhenDetectTenantAsyncButNoHeaderQueryOrBody_ThenReturnsNoTenantId()
        {
            var httpContext = new DefaultHttpContext
            {
                Request = { Method = HttpMethods.Get }
            };

            var result =
                await _detective.DetectTenantAsync(httpContext, typeof(TestTenantedRequest), CancellationToken.None);

            result.Should().BeSuccess();
            result.Value.TenantId.Should().BeNone();
            result.Value.ShouldHaveTenantId.Should().BeTrue();
        }
    }

    [UsedImplicitly]
    public class TestUnTenantedRequest : UnTenantedRequest<TestResponse>;

    [UsedImplicitly]
    public class TestTenantedRequest : TenantedRequest<TestResponse>;
}