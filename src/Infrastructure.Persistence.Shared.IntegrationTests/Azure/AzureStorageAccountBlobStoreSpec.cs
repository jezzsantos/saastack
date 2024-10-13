using FluentAssertions;
using Infrastructure.Persistence.Azure;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests.Azure;

[Trait("Category", "Integration.Persistence")]
[Collection("AzureStorageAccount")]
public class AzureStorageAccountBlobStoreSpec : AnyBlobStoreBaseSpec
{
    private readonly AzureStorageAccountSpecSetup _setup;

    public AzureStorageAccountBlobStoreSpec(AzureStorageAccountSpecSetup setup) : base(setup.BlobStore)
    {
        _setup = setup;
    }

    [Fact]
    public async Task WhenDownloadWithInvalidContainerName_ThenThrows()
    {
        await _setup.BlobStore
            .Invoking(
                x => x.DownloadAsync("^aninvalidcontainername^", "ablobname", Stream.Null, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidStorageAccountResourceName);
    }

    [Fact]
    public async Task WhenUploadWithInvalidContainerName_ThenThrows()
    {
        await _setup.BlobStore
            .Invoking(x =>
            {
                using var stream = new MemoryStream();
                return x.UploadAsync("^aninvalidcontainername^", "ablobname", "aconttenttype", stream,
                    CancellationToken.None);
            })
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidStorageAccountResourceName);
    }

    [Fact]
    public async Task WhenDestroyAllWithInvalidContainerName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.BlobStore
            .Invoking(x => x.DestroyAllAsync("^aninvalidcontainername^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidStorageAccountResourceName);
#endif
    }
}