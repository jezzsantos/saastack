using System.Net;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Gravatar;
using Infrastructure.Web.Interfaces.Clients;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices.External;

[Trait("Category", "Unit")]
public class GravatarClientSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly GravatarClient _client;
    private readonly Mock<IServiceClient> _serviceClient;

    public GravatarClientSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        _serviceClient = new Mock<IServiceClient>();

        _client = new GravatarClient(recorder.Object, _serviceClient.Object);
    }

    [Fact]
    public async Task WhenFindAvatarAsyncAndThrows_ThenReturnsNone()
    {
        _serviceClient.Setup(sc => sc.GetBinaryAsync(It.IsAny<ICallerContext>(), It.IsAny<IWebRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .Throws(new HttpRequestException("amessage"));

        var result =
            await _client.FindAvatarAsync(_caller.Object, "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenFindAvatarAsyncAndReturnsProblem_ThenReturnsNone()
    {
        _serviceClient.Setup(sc => sc.GetBinaryAsync(It.IsAny<ICallerContext>(), It.IsAny<IWebRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResponseProblem
            {
                Status = (int)HttpStatusCode.NotFound
            });

        var result =
            await _client.FindAvatarAsync(_caller.Object, "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenFindAvatarAsyncAndReturnsHttp404_ThenReturnsNone()
    {
        _serviceClient.Setup(sc => sc.GetBinaryAsync(It.IsAny<ICallerContext>(), It.IsAny<IWebRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BinaryResponse
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new MemoryStream(),
                ContentType = "acontenttype"
            });

        var result =
            await _client.FindAvatarAsync(_caller.Object, "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenFindAvatarAsyncAndReturnsImage_ThenReturnsUpload()
    {
        using var stream = new MemoryStream();
        _serviceClient.Setup(sc => sc.GetBinaryAsync(It.IsAny<ICallerContext>(), It.IsAny<IWebRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BinaryResponse
            {
                StatusCode = HttpStatusCode.OK,
                Content = stream,
                ContentType = HttpConstants.ContentTypes.ImageJpegWithCharset,
                ContentLength = 999
            });

        var result =
            await _client.FindAvatarAsync(_caller.Object, "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Value.ContentType.Should()
            .BeEquivalentTo(FileUploadContentType.FromContentType(HttpConstants.ContentTypes.ImageJpegWithCharset));
        result.Value.Value.Content.Should().BeSameAs(stream);
        result.Value.Value.Filename.Should().BeNull();
        result.Value.Value.Size.Should().Be(999);
        _serviceClient.Verify(sc => sc.GetBinaryAsync(_caller.Object, It.Is<GravatarGetImageRequest>(req =>
            req.Default == GravatarClient.DefaultImageBehaviour
        ), null, It.IsAny<CancellationToken>()));
    }
}