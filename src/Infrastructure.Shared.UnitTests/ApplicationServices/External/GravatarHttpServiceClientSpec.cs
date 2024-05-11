using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices.External;

[Trait("Category", "Unit")]
public class GravatarHttpServiceClientSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<GravatarHttpServiceClient.IGravatarClient> _client;
    private readonly GravatarHttpServiceClient _serviceClient;

    public GravatarHttpServiceClientSpec()
    {
        var recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _client = new Mock<GravatarHttpServiceClient.IGravatarClient>();

        _serviceClient = new GravatarHttpServiceClient(recorder.Object, _client.Object);
    }

    [Fact]
    public async Task WhenFindAvatarAsyncAndNone_ThenReturnsNone()
    {
        _client.Setup(ac => ac.FindAvatarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<FileUpload>.None);

        var result = await _serviceClient.FindAvatarAsync(_caller.Object, "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeNone();
        _client.Verify(ac => ac.FindAvatarAsync(It.IsAny<ICallerContext>(), "anemailaddress", CancellationToken.None));
    }

    [Fact]
    public async Task WhenFindAvatarAsync_ThenReturnsUpload()
    {
        using var stream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        _client.Setup(ac => ac.FindAvatarAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileUpload
            {
                ContentType = new FileUploadContentType { MediaType = "acontenttype" },
                Content = stream,
                Filename = "afilename",
                Size = 99
            }.ToOptional());

        var result = await _serviceClient.FindAvatarAsync(_caller.Object, "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.ContentType.MediaType.Should().Be("acontenttype");
        result.Value.Value.Content.Should().BeSameAs(stream);
        result.Value.Value.Filename.Should().Be("afilename");
        result.Value.Value.Size.Should().Be(99);
        _client.Verify(ac => ac.FindAvatarAsync(It.IsAny<ICallerContext>(), "anemailaddress", CancellationToken.None));
    }
}