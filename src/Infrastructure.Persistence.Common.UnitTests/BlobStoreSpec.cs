using Application.Persistence.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class BlobStoreSpec
{
    private readonly Mock<IBlobStore> _blobStore;
    private readonly BinaryBlobStore _store;

    public BlobStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        _blobStore = new Mock<IBlobStore>();
        _store = new BinaryBlobStore(recorder.Object, "acontainername", _blobStore.Object);
    }

    [Fact]
    public async Task WhenDeleteAndEmptyBlobName_ThenReturnsError()
    {
        var result = await _store.DestroyAsync(string.Empty, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public async Task WhenDelete_ThenDeletesFromStore()
    {
        await _store.DestroyAsync("ablobname", CancellationToken.None);

        _blobStore.Verify(store => store.DeleteAsync("acontainername", "ablobname", CancellationToken.None));
    }

#if TESTINGONLY
    [Fact]
    public async Task WhenDestroyAll_ThenDestroysAllInStore()
    {
        await _store.DestroyAllAsync(CancellationToken.None);

        _blobStore.Verify(store => store.DestroyAllAsync("acontainername", CancellationToken.None));
    }
#endif

    [Fact]
    public async Task WhenGetAndEmptyBlobName_ThenReturnsError()
    {
        using var stream = new MemoryStream();

        var result = await _store.GetAsync(string.Empty, stream, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public async Task WhenGetAndNotExists_ThenReturnsNone()
    {
        var stream = new MemoryStream();
        _blobStore.Setup(blo => blo.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<Blob>.None);

        var result = await _store.GetAsync("ablobname", stream, CancellationToken.None);

        result.Value.Should().BeNone();
        _blobStore.Verify(store => store.DownloadAsync("acontainername", "ablobname", stream, CancellationToken.None));
    }

    [Fact]
    public async Task WhenGet_ThenDownloadsFromStore()
    {
        var stream = new MemoryStream();
        _blobStore.Setup(blo => blo.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Stream>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Blob { ContentType = "acontenttype" }.ToOptional());

        var result = await _store.GetAsync("ablobname", stream, CancellationToken.None);

        result.Value.Value.ContentType.Should().Be("acontenttype");
        _blobStore.Verify(store => store.DownloadAsync("acontainername", "ablobname", stream, CancellationToken.None));
    }

    [Fact]
    public async Task WhenSaveAndEmptyBlobName_ThenReturnsError()
    {
        var stream = new MemoryStream([0x01]);

        var result = await _store.SaveAsync(string.Empty, "acontenttype", stream, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public async Task WhenSaveAndEmptyContentType_ThenReturnsError()
    {
        var stream = new MemoryStream([0x01]);

        var result = await _store.SaveAsync("ablobname", string.Empty, stream, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.BlobStore_EmptyContentType);
    }

    [Fact]
    public async Task WhenSave_ThenUploadsToStore()
    {
        var stream = new MemoryStream([0x01]);

        await _store.SaveAsync("ablobname", "acontenttype", stream, CancellationToken.None);

        _blobStore.Verify(store =>
            store.UploadAsync("acontainername", "ablobname", "acontenttype", stream, CancellationToken.None));
    }
}