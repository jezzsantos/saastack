using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class WebRequestSpec
{
    [Trait("Category", "Unit")]
    public class GivenAJsonRequest
    {
        [Fact]
        public async Task WhenBindAsyncAndEmptyHttpRequest_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream("{}"u8.ToArray())
                }
            };

            var result = await TestRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().BeNull();
            result.ANumberProperty.Should().Be(0);
            result.AStringProperty.Should().BeNull();
        }

        [Fact]
        public async Task WhenBindAsyncAndPropertiesInQueryString_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream("{}"u8.ToArray()),
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(TestRequest.Id), "anid" },
                        { nameof(TestRequest.ANumberProperty), "999" },
                        { nameof(TestRequest.AStringProperty), "avalue" }
                    })
                }
            };

            var result = await TestRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndPropertiesInRouteValues_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream("{}"u8.ToArray()),
                    RouteValues = new RouteValueDictionary
                    {
                        { nameof(TestRequest.Id), "anid" },
                        { nameof(TestRequest.ANumberProperty), "999" },
                        { nameof(TestRequest.AStringProperty), "avalue" }
                    }
                }
            };

            var result = await TestRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAMultiPartFormRequest
    {
        [Fact]
        public async Task WhenBindAsyncAndEmptyHttpRequest_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    Form = new FormCollection(new Dictionary<string, StringValues>())
                }
            };

            var result = await TestMultiPartRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().BeNull();
            result.ANumberProperty.Should().Be(0);
            result.AStringProperty.Should().BeNull();
        }

        [Fact]
        public async Task WhenBindAsyncAndPropertiesInQueryString_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData + "; boundary=boundary",
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(TestRequest.Id), "anid" },
                        { nameof(TestRequest.ANumberProperty), "999" },
                        { nameof(TestRequest.AStringProperty), "avalue" }
                    })
                }
            };

            var result = await TestMultiPartRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndPropertiesInRouteValues_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData + "; boundary=boundary",
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    RouteValues = new RouteValueDictionary
                    {
                        { nameof(TestRequest.Id), "anid" },
                        { nameof(TestRequest.ANumberProperty), "999" },
                        { nameof(TestRequest.AStringProperty), "avalue" }
                    }
                }
            };

            var result = await TestMultiPartRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndPropertiesInForm_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData + "; boundary=boundary",
                    Form = new FormCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(TestRequest.Id), "anid" },
                        { nameof(TestRequest.ANumberProperty), "999" },
                        { nameof(TestRequest.AStringProperty), "avalue" }
                    })
                }
            };

            var result = await TestMultiPartRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }
    }
}

[Route("/aroute", OperationMethod.Get)]
[UsedImplicitly]
public class TestMultiPartRequest : WebRequest<TestMultiPartRequest, TestResponse>, IHasMultipartForm
{
    public int ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }

    public string? Id { get; set; }
}