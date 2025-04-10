using Common.Extensions;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.External.Persistence.IntegrationTests;

public abstract class AnyDataBlobStoreBaseSpec
{
    private readonly BlobStoreInfo _setup;

    protected AnyDataBlobStoreBaseSpec(IBlobStore blobStore)
    {
        _setup = new BlobStoreInfo
            { Store = blobStore, ContainerName = typeof(TestDataBlobStoreEntity).GetEntityNameSafe() };
#if TESTINGONLY
        _setup.Store.DestroyAllAsync(_setup.ContainerName, CancellationToken.None).GetAwaiter()
            .GetResult();
#endif
    }

    [Fact]
    public async Task WhenDownloadWithNullContainerName_ThenThrows()
    {
        await _setup.Store
            .Invoking(x => x.DownloadAsync(null!, "ablobname", new MemoryStream(), CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenDownloadWithNullBlobName_ThenThrows()
    {
        await _setup.Store
            .Invoking(x =>
                x.DownloadAsync(_setup.ContainerName, null!, new MemoryStream(), CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenDownloadWithNullStream_ThenThrows()
    {
        await _setup.Store
            .Invoking(x => x.DownloadAsync(_setup.ContainerName, "ablobname", null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenDownloadAndContainerNotExists_ThenReturnsNone()
    {
        using var stream = new MemoryStream();
        var result = await _setup.Store.DownloadAsync(_setup.ContainerName, "ablobname",
            stream, CancellationToken.None);

        result.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenDownloadAndBlobNotExists_ThenReturnsNone()
    {
        var data = new byte[] { 0x00, 0x01, 0x02 };
        using var stream = new MemoryStream(data);

        const string contentType = "image/bmp";
        await _setup.Store.UploadAsync(_setup.ContainerName, "ablobname", contentType, stream, CancellationToken.None);
        using var downloaded = new MemoryStream();

        var result = await _setup.Store.DownloadAsync(_setup.ContainerName,
            "adifferentblobname", downloaded, CancellationToken.None);

        result.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenDownloadAndExists_ThenReturnsBlob()
    {
        var data = new byte[] { 0x00, 0x01, 0x02 };
        using var stream = new MemoryStream(data);
        const string contentType = "image/bmp";
        await _setup.Store.UploadAsync(_setup.ContainerName, "ablobname", contentType, stream,
            CancellationToken.None);
        using var downloaded = new MemoryStream();

        var result = await _setup.Store.DownloadAsync(_setup.ContainerName, "ablobname", downloaded,
            CancellationToken.None);

        result.Value.Value.ContentType.Should().Be(contentType);
        downloaded.Rewind();
        downloaded.ReadFully().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WhenUploadAndContainerNameIsNull_ThenThrows()
    {
        const string contentType = "image/bmp";

        await _setup.Store
            .Invoking(x =>
            {
                using var stream = new MemoryStream();
                return x.UploadAsync(null!, "ablobname", contentType, stream, CancellationToken.None);
            })
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenUploadAndBlobNameIsNull_ThenThrows()
    {
        const string contentType = "image/bmp";

        await _setup.Store
            .Invoking(x =>
            {
                using var stream = new MemoryStream();
                return x.UploadAsync(_setup.ContainerName, null!, contentType, stream, CancellationToken.None);
            })
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenUploadAndContentTypeIsNull_ThenThrows()
    {
        await _setup.Store
            .Invoking(x =>
            {
                using var stream = new MemoryStream();
                return x.UploadAsync(_setup.ContainerName, "ablobname", null!, stream, CancellationToken.None);
            })
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenUploadAndStreamIsNull_ThenThrows()
    {
        const string contentType = "image/bmp";

        await _setup.Store
            .Invoking(x =>
                x.UploadAsync(_setup.ContainerName, "ablobname", contentType, null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenUploadAndExists_ThenOverwrites()
    {
        var data = new byte[] { 0x00, 0x01, 0x02 };
        using var stream = new MemoryStream(data);
        const string contentType = "image/bmp";
        await _setup.Store.UploadAsync(_setup.ContainerName, "ablobname", contentType, stream, CancellationToken.None);
        var newData = new byte[] { 0x03, 0x04, 0x05 };
        using var newStream = new MemoryStream(newData);

        const string newContentType = "application/gzip";
        await _setup.Store.UploadAsync(_setup.ContainerName, "ablobname", newContentType, newStream,
            CancellationToken.None);

        using var downloaded = new MemoryStream();
        var result = await _setup.Store.DownloadAsync(_setup.ContainerName, "ablobname", downloaded,
            CancellationToken.None);

        result.Value.Value.ContentType.Should().Be(newContentType);
        downloaded.Rewind();
        downloaded.ReadFully().Should().BeEquivalentTo(newData);
    }

    [Fact]
#pragma warning disable S4144
    public async Task WhenUploadAndNotExists_ThenAddsNewBlob()
#pragma warning restore S4144
    {
        var data = new byte[] { 0x00, 0x01, 0x02 };
        using var stream = new MemoryStream(data);
        const string contentType = "image/bmp";
        await _setup.Store.UploadAsync(_setup.ContainerName, "ablobname", contentType, stream,
            CancellationToken.None);

        using var downloaded = new MemoryStream();
        var result = await _setup.Store.DownloadAsync(_setup.ContainerName, "ablobname", downloaded,
            CancellationToken.None);

        result.Value.Value.ContentType.Should().Be(contentType);
        downloaded.Rewind();
        downloaded.ReadFully().Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task WhenDeleteAndContainerNameIsNull_ThenThrows()
    {
        await _setup.Store
            .Invoking(x =>
            {
                using var stream = new MemoryStream();
                return x.DeleteAsync(null!, "ablobname", CancellationToken.None);
            })
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenDeleteAndBlobNameIsNull_ThenThrows()
    {
        await _setup.Store
            .Invoking(x =>
            {
                using var stream = new MemoryStream();
                return x.DeleteAsync(_setup.ContainerName, null!, CancellationToken.None);
            })
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenDeleteAndBlobNotExists_ThenDeletes()
    {
        var data = new byte[] { 0x00, 0x01, 0x02 };
        using var stream = new MemoryStream(data);
        const string contentType = "image/bmp";
        await _setup.Store.UploadAsync(_setup.ContainerName, "ablobname", contentType, stream,
            CancellationToken.None);

        await _setup.Store
            .Invoking(x => x.DeleteAsync(_setup.ContainerName, "adifferentblobname", CancellationToken.None))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task WhenDelete_ThenDeletesBlob()
    {
        var data = new byte[] { 0x00, 0x01, 0x02 };
        using var stream = new MemoryStream(data);
        const string contentType = "image/bmp";
        await _setup.Store.UploadAsync(_setup.ContainerName, "ablobname", contentType, stream,
            CancellationToken.None);

        await _setup.Store.DeleteAsync(_setup.ContainerName, "adifferentblobname",
            CancellationToken.None);

        using var downloaded = new MemoryStream();
        var result = await _setup.Store.DownloadAsync(_setup.ContainerName,
            "adifferentblobname", downloaded, CancellationToken.None);

        result.Value.Should().BeNone();
    }

    public class BlobStoreInfo
    {
        public required string ContainerName { get; set; }

        public required IBlobStore Store { get; set; }
    }

    protected string ContainerName => _setup.ContainerName;
}