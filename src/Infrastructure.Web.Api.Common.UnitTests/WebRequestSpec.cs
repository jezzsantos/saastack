using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[UsedImplicitly]
public class WebRequestSpec
{
    [Trait("Category", "Unit")]
    public class GivenAJsonRequest
    {
        private readonly Mock<IServiceProvider> _serviceProvider;

        public GivenAJsonRequest()
        {
            _serviceProvider = new Mock<IServiceProvider>();
            _serviceProvider.Setup(sp => sp.GetService(typeof(JsonSerializerOptions)))
                .Returns(JsonSerializerOptions.Default);
        }

        [Fact]
        public async Task WhenBindAsyncAndEmptyHttpRequest_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream("{}"u8.ToArray())
                },
                RequestServices = _serviceProvider.Object
            };

            var result = await TestJsonRequest.BindAsync(context, null!);

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
                        { nameof(TestJsonRequest.Id), "anid" },
                        { nameof(TestJsonRequest.ANumberProperty), "999" },
                        { nameof(TestJsonRequest.AStringProperty), "avalue" }
                    })
                },
                RequestServices = _serviceProvider.Object
            };

            var result = await TestJsonRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInQueryString_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream("{}"u8.ToArray()),
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    })
                },
                RequestServices = _serviceProvider.Object
            };

            var result = await TestAliasedJsonRequest.BindAsync(context, null!);

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
                        { nameof(TestJsonRequest.Id), "anid" },
                        { nameof(TestJsonRequest.ANumberProperty), "999" },
                        { nameof(TestJsonRequest.AStringProperty), "avalue" }
                    }
                },
                RequestServices = _serviceProvider.Object
            };

            var result = await TestJsonRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInRouteValues_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.Json,
                    Body = new MemoryStream("{}"u8.ToArray()),
                    RouteValues = new RouteValueDictionary
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    }
                },
                RequestServices = _serviceProvider.Object
            };

            var result = await TestAliasedJsonRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAMultiPartFormDataRequest
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

            var result = await TestMultipartFormDataRequest.BindAsync(context, null!);

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
                        { nameof(TestMultipartFormDataRequest.Id), "anid" },
                        { nameof(TestMultipartFormDataRequest.ANumberProperty), "999" },
                        { nameof(TestMultipartFormDataRequest.AStringProperty), "avalue" }
                    })
                }
            };

            var result = await TestMultipartFormDataRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInQueryString_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData + "; boundary=boundary",
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    })
                }
            };

            var result = await TestAliasedMultipartFormDataRequest.BindAsync(context, null!);

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
                        { nameof(TestMultipartFormDataRequest.Id), "anid" },
                        { nameof(TestMultipartFormDataRequest.ANumberProperty), "999" },
                        { nameof(TestMultipartFormDataRequest.AStringProperty), "avalue" }
                    }
                }
            };

            var result = await TestMultipartFormDataRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInRouteValues_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData + "; boundary=boundary",
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    RouteValues = new RouteValueDictionary
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    }
                }
            };

            var result = await TestAliasedMultipartFormDataRequest.BindAsync(context, null!);

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
                        { nameof(TestMultipartFormDataRequest.Id), "anid" },
                        { nameof(TestMultipartFormDataRequest.ANumberProperty), "999" },
                        { nameof(TestMultipartFormDataRequest.AStringProperty), "avalue" }
                    })
                }
            };

            var result = await TestMultipartFormDataRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInForm_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData + "; boundary=boundary",
                    Form = new FormCollection(new Dictionary<string, StringValues>
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    })
                }
            };

            var result = await TestAliasedMultipartFormDataRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAFormUrlEncodedRequest
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

            var result = await TestFormUrlEncodedRequest.BindAsync(context, null!);

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
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(TestFormUrlEncodedRequest.Id), "anid" },
                        { nameof(TestFormUrlEncodedRequest.ANumberProperty), "999" },
                        { nameof(TestFormUrlEncodedRequest.AStringProperty), "avalue" }
                    })
                }
            };

            var result = await TestFormUrlEncodedRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInQueryString_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    Query = new QueryCollection(new Dictionary<string, StringValues>
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    })
                }
            };

            var result = await TestAliasedFormUrlEncodedRequest.BindAsync(context, null!);

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
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    RouteValues = new RouteValueDictionary
                    {
                        { nameof(TestFormUrlEncodedRequest.Id), "anid" },
                        { nameof(TestFormUrlEncodedRequest.ANumberProperty), "999" },
                        { nameof(TestFormUrlEncodedRequest.AStringProperty), "avalue" }
                    }
                }
            };

            var result = await TestFormUrlEncodedRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInRouteValues_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.FormUrlEncoded,
                    Form = new FormCollection(new Dictionary<string, StringValues>()),
                    RouteValues = new RouteValueDictionary
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    }
                }
            };

            var result = await TestAliasedFormUrlEncodedRequest.BindAsync(context, null!);

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
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData,
                    Form = new FormCollection(new Dictionary<string, StringValues>
                    {
                        { nameof(TestFormUrlEncodedRequest.Id), "anid" },
                        { nameof(TestFormUrlEncodedRequest.ANumberProperty), "999" },
                        { nameof(TestFormUrlEncodedRequest.AStringProperty), "avalue" }
                    })
                }
            };

            var result = await TestFormUrlEncodedRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }

        [Fact]
        public async Task WhenBindAsyncAndAliasedPropertiesInForm_ThenReturnsInstance()
        {
            var context = new DefaultHttpContext
            {
                Request =
                {
                    ContentType = HttpConstants.ContentTypes.MultiPartFormData,
                    Form = new FormCollection(new Dictionary<string, StringValues>
                    {
                        { "id", "anid" },
                        { "a_number_property", "999" },
                        { "a_string_property", "avalue" }
                    })
                }
            };

            var result = await TestAliasedFormUrlEncodedRequest.BindAsync(context, null!);

            result.Should().NotBeNull();
            result!.Id.Should().Be("anid");
            result.ANumberProperty.Should().Be(999);
            result.AStringProperty.Should().Be("avalue");
        }
    }
}

[Route("/aroute", OperationMethod.Get)]
[UsedImplicitly]
public class TestJsonRequest : WebRequest<TestJsonRequest, TestResponse>
{
    public int ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }

    public string? Id { get; set; }
}

[Route("/aroute", OperationMethod.Get)]
[UsedImplicitly]
public class TestAliasedJsonRequest : WebRequest<TestAliasedJsonRequest, TestResponse>
{
    [JsonPropertyName("a_number_property")]
    public int ANumberProperty { get; set; }

    [JsonPropertyName("a_string_property")]
    public string? AStringProperty { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }
}

[Route("/aroute", OperationMethod.Post)]
[UsedImplicitly]
public class TestMultipartFormDataRequest : WebRequest<TestMultipartFormDataRequest, TestResponse>,
    IHasMultipartFormData
{
    public int ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }

    public string? Id { get; set; }
}

[Route("/aroute", OperationMethod.Post)]
[UsedImplicitly]
public class TestAliasedMultipartFormDataRequest : WebRequest<TestAliasedMultipartFormDataRequest, TestResponse>,
    IHasMultipartFormData
{
    [JsonPropertyName("a_number_property")]
    public int ANumberProperty { get; set; }

    [JsonPropertyName("a_string_property")]
    public string? AStringProperty { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }
}

[Route("/aroute", OperationMethod.Post)]
[UsedImplicitly]
public class TestFormUrlEncodedRequest : WebRequest<TestFormUrlEncodedRequest, TestResponse>,
    IHasFormUrlEncoded
{
    public int ANumberProperty { get; set; }

    public string? AStringProperty { get; set; }

    public string? Id { get; set; }
}

[Route("/aroute", OperationMethod.Post)]
[UsedImplicitly]
public class TestAliasedFormUrlEncodedRequest : WebRequest<TestAliasedFormUrlEncodedRequest, TestResponse>,
    IHasFormUrlEncoded
{
    [JsonPropertyName("a_number_property")]
    public int ANumberProperty { get; set; }

    [JsonPropertyName("a_string_property")]
    public string? AStringProperty { get; set; }

    [JsonPropertyName("id")] public string? Id { get; set; }
}