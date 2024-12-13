using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Interfaces.Clients;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Moq.Protected;
using Xunit;
using HttpClient = System.Net.Http.HttpClient;

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
                        ContentType = new MediaTypeHeaderValue(HttpConstants.ContentTypes.Html)
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
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndNoContentTypeForFailureAndRfc7808Error_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new ProblemDetails
                {
                    Title = "atitle",
                    Type = "atype",
                    Detail = "adetail",
                    Instance = "aninstance",
                    Status = 999,
                    Extensions = { { "aname", "avalue" } }
                }.ToJson()!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("atitle");
            result.Error.Detail.Should().Be("adetail");
            result.Error.Type.Should().Be("atype");
            result.Error.Instance.Should().Be("aninstance");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should()
                .OnlyContain(pair => pair.Key == "aname" && pair.Value.As<JsonElement>().GetString() == "avalue");
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndNoContentTypeForFailureAndRfc6749Error_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new
                {
                    error = "anerror",
                    error_description = "anerrordescription",
                    error_uri = "anerroruri",
                    state = "astate"
                }.ToJson()!),
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
            result.Error.Extensions.Should().BeNull();
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
                }, new MediaTypeHeaderValue(HttpConstants.ContentTypes.JsonProblem)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("atitle");
            result.Error.Detail.Should().Be("adetail");
            result.Error.Type.Should().Be("atype");
            result.Error.Instance.Should().Be("aninstance");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should()
                .OnlyContain(pair => pair.Key == "aname" && pair.Value.As<JsonElement>().GetString() == "avalue");
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
                }, new MediaTypeHeaderValue(HttpConstants.ContentTypes.Json))
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
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndContentTypeIsJsonAndContentForRfc7808Error_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = JsonContent.Create(new
                {
                    title = "atitle",
                    type = "atype",
                    detail = "adetail",
                    instance = "aninstance",
                    status = 999,
                    aname = "avalue" // Note all extensions are on the root!
                }, new MediaTypeHeaderValue(HttpConstants.ContentTypes.Json)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("atitle");
            result.Error.Detail.Should().Be("adetail");
            result.Error.Type.Should().Be("atype");
            result.Error.Instance.Should().Be("aninstance");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should()
                .OnlyContain(pair => pair.Key == "aname" && pair.Value.As<JsonElement>().GetString() == "avalue");
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
                }, new MediaTypeHeaderValue(HttpConstants.ContentTypes.Json)),
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
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task
            WhenGetTypedResponseAsyncAndContentTypeIsJsonAndNonStandardError1ForFailure_ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new
                {
                    unknown = "anunknown"
                }.ToJson(false)!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be(Resources.JsonClient_TryParseNonStandardErrors_NonStandard);
            result.Error.Detail.Should().Be("{\"unknown\":\"anunknown\"}");
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task
            WhenGetTypedResponseAsyncAndContentTypeIsJsonAndNonStandardError2ForFailure1ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new NonStandardProblemDetails
                {
                    Error = new NonStandardProblemError
                    {
                        Code = "acode",
                        Message = "amessage"
                    }
                }.ToJson()!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("acode");
            result.Error.Detail.Should().Be("amessage");
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task
            WhenGetTypedResponseAsyncAndContentTypeIsJsonAndNonStandardError3ForFailure1ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new NonStandardProblemDetails
                {
                    Error = new NonStandardProblemError
                    {
                        Reason = "areason",
                        Description = "adescription"
                    }
                }.ToJson()!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().Be("adescription");
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndContentTypeIsTextForSuccess_ThenReturnsEmptyResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("acontent", new MediaTypeHeaderValue(HttpConstants.ContentTypes.Text))
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.Value.AStringProperty.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetTypedResponseAsyncAndContentTypeIsTextForFailure_ThenReturnsEmptyResponse()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content =
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue(HttpConstants.ContentTypes.Text)
                    }
                }
            };

            var result =
                await JsonClient.GetTypedResponseAsync<TestResponse>(response, null, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("Internal Server Error");
            result.Error.Detail.Should().BeNull();
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndGetMethodWithEmptyRequest_ThenContentIsEmpty()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestRequest
            {
                AProperty = null!
            };

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Get, request, null, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri == new Uri("http://localhost/test")
                        && req.Content == null
                    ),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndGetMethodWithValue_ThenContentIsEmpty()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestRequest
            {
                AProperty = "avalue"
            };

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Get, request, null, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri == new Uri("http://localhost/test?aproperty=avalue")
                        && req.Content == null
                    ),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndGetMethodWithArray_ThenContentIsEmpty()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestRequest
            {
                AProperty = null!,
                AnArrayProperty = new[] { "anothervalue1", "anothervalue2" }
            };

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Get, request, null, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri
                        == new Uri("http://localhost/test?anarrayproperty=anothervalue1&anarrayproperty=anothervalue2")
                        && req.Content == null
                    ),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndGetMethodWithMultipleProperties_ThenContentIsEmpty()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestRequest
            {
                AProperty = "avalue",
                AnArrayProperty = new[] { "anothervalue1", "anothervalue2" }
            };

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Get, request, null, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Get
                        && req.RequestUri
                        == new Uri(
                            "http://localhost/test?anarrayproperty=anothervalue1&anarrayproperty=anothervalue2&aproperty=avalue")
                        && req.Content == null
                    ),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndPostMethodWithJsonRequest_ThenContentIsStringContent()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestRequest();

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Post, request, null, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.Content is StringContent
                    ),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndPostMethodWithIMultiPartFormRequest_ThenContentIsMultiPartForm()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestMultiPartFormDataRequest();
            var file = new PostFile(new MemoryStream(), HttpConstants.ContentTypes.Json, "afilename");

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Post, request, file, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.Content is MultipartContent
                    ),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndPostMethodWithFile_ThenContentIsMultiPartFormWithFile()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestRequest();
            var file = new PostFile(new MemoryStream(), HttpConstants.ContentTypes.Json, "afilename");

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Post, request, file, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.Content is MultipartContent
                    ),
                    ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task WhenSendRequestAsyncAndPostMethodWithIHasUrlEncodedFormRequest_ThenContentIsUrlEncodedForm()
        {
            var response = new HttpResponseMessage();
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
            var client = new HttpClient(handler.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            var request = new TestFormUrlEncodedRequest();

            var result =
                await JsonClient.SendRequestAsync(client, HttpMethod.Post, request, null, null,
                    CancellationToken.None);

            result.Should().Be(response);
            handler.Protected()
                .Verify("SendAsync", Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post
                        && req.Content is FormUrlEncodedContent
                    ),
                    ItExpr.IsAny<CancellationToken>());
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
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

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
                        ContentType = new MediaTypeHeaderValue(HttpConstants.ContentTypes.Html)
                    }
                }
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

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
                Content = new StringContent("acontent", new MediaTypeHeaderValue(HttpConstants.ContentTypes.Text))
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeTrue();
            result.Value.Should().Be("acontent");
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndNoContentTypeForFailureAndNoError_ThenReturnsProblem()
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
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().BeNull();
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndNoContentTypeForFailureAndRfc7808Error_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new ProblemDetails
                {
                    Title = "atitle",
                    Type = "atype",
                    Detail = "adetail",
                    Instance = "aninstance",
                    Status = 999,
                    Extensions = { { "aname", "avalue" } }
                }.ToJson()!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("atitle");
            result.Error.Detail.Should().Be("adetail");
            result.Error.Type.Should().Be("atype");
            result.Error.Instance.Should().Be("aninstance");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should()
                .OnlyContain(pair => pair.Key == "aname" && pair.Value.As<JsonElement>().GetString() == "avalue");
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndNoContentTypeForFailureAndRfc6749Error_ThenReturnsProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new
                {
                    error = "anerror",
                    error_description = "anerrordescription",
                    error_uri = "anerroruri",
                    state = "astate"
                }.ToJson()!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("anerror");
            result.Error.Detail.Should().Be("anerrordescription");
            result.Error.Type.Should().Be(OAuth2Rfc6749ProblemDetails.Reference);
            result.Error.Instance.Should().Be("anerroruri");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
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
                }, new MediaTypeHeaderValue(HttpConstants.ContentTypes.JsonProblem)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("atitle");
            result.Error.Detail.Should().Be("adetail");
            result.Error.Type.Should().Be("atype");
            result.Error.Instance.Should().Be("aninstance");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should()
                .OnlyContain(pair => pair.Key == "aname" && pair.Value.As<JsonElement>().GetString() == "avalue");
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
                }, new MediaTypeHeaderValue(HttpConstants.ContentTypes.Json))
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

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
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().BeNull();
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task
            WhenGetStringResponseAsyncAndContentTypeIsJsonAndNonStandardError1ForFailure1ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new
                {
                    unknown = "anunknown"
                }.ToJson(false)!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be(Resources.JsonClient_TryParseNonStandardErrors_NonStandard);
            result.Error.Detail.Should().Be("{\"unknown\":\"anunknown\"}");
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task
            WhenGetStringResponseAsyncAndContentTypeIsJsonAndNonStandardError2ForFailure_ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new NonStandardProblemDetails
                {
                    Error = new NonStandardProblemError
                    {
                        Code = "acode",
                        Message = "amessage"
                    }
                }.ToJson()!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("acode");
            result.Error.Detail.Should().Be("amessage");
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task
            WhenGetStringResponseAsyncAndContentTypeIsJsonAndNonStandardError3ForFailure1ThenReturnsResponseProblem()
        {
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(new NonStandardProblemDetails
                {
                    Error = new NonStandardProblemError
                    {
                        Reason = "areason",
                        Description = "adescription"
                    }
                }.ToJson()!),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("areason");
            result.Error.Detail.Should().Be("adescription");
            result.Error.Type.Should().BeNull();
            result.Error.Instance.Should().BeNull();
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
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
                }, new MediaTypeHeaderValue(HttpConstants.ContentTypes.Json)),
                ReasonPhrase = "areason"
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeFalse();
            result.Error.Status.Should().Be(500);
            result.Error.Title.Should().Be("anerror");
            result.Error.Detail.Should().Be("anerrordescription");
            result.Error.Type.Should().Be(OAuth2Rfc6749ProblemDetails.Reference);
            result.Error.Instance.Should().Be("anerroruri");
            result.Error.Exception.Should().BeNull();
            result.Error.Errors.Should().BeNull();
            result.Error.Extensions.Should().BeNull();
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndContentTypeIsImageForSuccess_ThenReturnsResponse()
        {
            using var stream = new MemoryStream();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue(HttpConstants.ContentTypes.ImagePng)
                    }
                }
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeFalse();
        }

        [Fact]
        public async Task WhenGetStringResponseAsyncAndContentTypeIsFileForSuccess_ThenReturnsResponse()
        {
            using var stream = new MemoryStream();
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StreamContent(stream)
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue(HttpConstants.ContentTypes.OctetStream)
                    }
                }
            };

            var result =
                await JsonClient.GetStringResponseAsync(response, CancellationToken.None);

            result.IsSuccessful.Should().BeTrue();
            result.HasValue.Should().BeFalse();
        }
    }
}

[Api.Interfaces.Route("/test", OperationMethod.Get)]
public class TestRequest : WebRequest<TestRequest>
{
    [FromQuery] public string[]? AnArrayProperty { get; set; }

    public string AProperty { get; set; } = "avalue";
}

[Api.Interfaces.Route("/test", OperationMethod.Post)]
public class TestMultiPartFormDataRequest : WebRequest<TestRequest>, IHasMultipartFormData
{
    public string AProperty { get; set; } = "avalue";
}

[Api.Interfaces.Route("/test", OperationMethod.Post)]
public class TestFormUrlEncodedRequest : WebRequest<TestRequest>, IHasFormUrlEncoded
{
    public string AProperty { get; set; } = "avalue";
}

public class TestResponse : IWebResponse
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? AStringProperty { get; set; }
}