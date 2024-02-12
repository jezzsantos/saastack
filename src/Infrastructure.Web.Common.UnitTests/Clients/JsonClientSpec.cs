using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Clients;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Infrastructure.Web.Common.UnitTests.Clients;

[UsedImplicitly]
public class JsonClientSpec
{
    [Trait("Category", "Unit")]
    public class GivenATypedResponse
    {
        [Fact]
        public async Task WhenGetTypedResponseAsyncAndNoContentTypeForSuccess_ThenReturnsEmptyResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                {
                    Headers =
                    {
                        ContentType = null
                    }
                }
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(new TestResponse());
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndNotJsonContentTypeForSuccess_ThenReturnsEmptyResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue(HttpContentTypes.Html)
                    }
                }
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(new TestResponse());
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndNoContentTypeForFailure_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content =
                {
                    Headers =
                    {
                        ContentType = null
                    }
                },
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().BeNull();
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndContentTypeIsJsonProblemForFailure_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = JsonContent.Create(new ProblemDetails
                {
                    Title = "atitle",
                    Type = "atype",
                    Detail = "adetail",
                    Instance = "aninstance",
                    Status = 999,
                    Extensions = { { "aname", "avalue" } }
                }, new MediaTypeHeaderValue(HttpContentTypes.JsonProblem)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(999);
            result.Error.Title.Should().Be("atitle");
            result.Error.Detail.Should().Be("adetail");
            result.Error.Type.Should().Be("atype");
            result.Error.Instance.Should().Be("aninstance");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndContentTypeIsJsonForSuccess_ThenReturnsResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new
                {
                    AStringProperty = "astringproperty",
                    AnOtherProperty = "anotherproperty"
                }, new MediaTypeHeaderValue(HttpContentTypes.Json))
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.Value.AStringProperty.Should().Be("astringproperty");
        }

        [Fact]
        public async Task
            WhenGetTypedResponseAsyncAndContentTypeIsJsonAndNoContentForFailure_ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().BeNull();
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndContentTypeIsJsonAndContentForRfc6749_ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = JsonContent.Create(new
                {
                    error = "anerror",
                    error_description = "anerrordescription",
                    error_uri = "anerroruri",
                    state = "astate"
                }, new MediaTypeHeaderValue(HttpContentTypes.Json)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("anerror");
            result.Error.Detail.Should().Be("anerrordescription");
            result.Error.Type.Should().Be(OAuth2Rfc6749ProblemDetails.Reference);
            result.Error.Instance.Should().Be("anerroruri");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAnUntypedResponse
    {
        [Fact]
        public async Task WhenGetStringResponseAsyncAndNoContentTypeForSuccess_ThenReturnsEmptyResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                {
                    Headers =
                    {
                        ContentType = null
                    }
                }
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeFalse();
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndEmptyTextContentTypeForSuccess_ThenReturnsEmptyResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content =
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue(HttpContentTypes.Html)
                    }
                }
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeTrue();
            result.Value.Should().Be(string.Empty);
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndTextContentTypeForSuccess_ThenReturnsEmptyResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("acontent", new MediaTypeHeaderValue(HttpContentTypes.Text))
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeTrue();
            result.Value.Should().Be("acontent");
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndNoContentTypeForFailure_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content =
                {
                    Headers =
                    {
                        ContentType = null
                    }
                },
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().BeNull();
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndContentTypeIsJsonProblemForFailure_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = JsonContent.Create(new ProblemDetails
                {
                    Title = "atitle",
                    Type = "atype",
                    Detail = "adetail",
                    Instance = "aninstance",
                    Status = 999,
                    Extensions = { { "aname", "avalue" } }
                }, new MediaTypeHeaderValue(HttpContentTypes.JsonProblem)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(999);
            result.Error.Title.Should().Be("atitle");
            result.Error.Detail.Should().Be("adetail");
            result.Error.Type.Should().Be("atype");
            result.Error.Instance.Should().Be("aninstance");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndContentTypeIsJsonForSuccess_ThenReturnsResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new
                {
                    AStringProperty = "astringproperty",
                    AnOtherProperty = "anotherproperty"
                }, new MediaTypeHeaderValue(HttpContentTypes.Json))
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.Value.Should()
                .Be("{\"aStringProperty\":\"astringproperty\",\"anOtherProperty\":\"anotherproperty\"}");
        }

        [Fact]
        public async Task
            WhenGetStringResponseAsyncAndContentTypeIsJsonAndNoContentForFailure_ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().BeNull();
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }

        [Fact]
        public async Task
            WhenGetStringResponseAsyncAndContentTypeIsJsonAndContentForRfc6749_ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = JsonContent.Create(new
                {
                    error = "anerror",
                    error_description = "anerrordescription",
                    error_uri = "anerroruri",
                    state = "astate"
                }, new MediaTypeHeaderValue(HttpContentTypes.Json)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("anerror");
            result.Error.Detail.Should().Be("anerrordescription");
            result.Error.Type.Should().Be(OAuth2Rfc6749ProblemDetails.Reference);
            result.Error.Instance.Should().Be("anerroruri");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
        }
    }
}

public class TestResponse : IWebResponse
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? AStringProperty { get; set; }
}