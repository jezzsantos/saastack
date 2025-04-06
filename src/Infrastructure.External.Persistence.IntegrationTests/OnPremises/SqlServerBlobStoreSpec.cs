using FluentAssertions;
using Infrastructure.External.Persistence.OnPremises;
using JetBrains.Annotations;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.External.Persistence.IntegrationTests.OnPremises;

[Trait("Category", "Integration.Persistence")]
[Collection("SqlServerStore")]
[UsedImplicitly]
public class SqlServerBlobStoreSpec : AnyDataBlobStoreBaseSpec
{
    private readonly SqlServerStorageSpecSetup _setup;

    public SqlServerBlobStoreSpec(SqlServerStorageSpecSetup setup) : base(setup.BlobStore)
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
            .WithMessageLike(Resources.ValidationExtensions_InvalidDatabaseResourceName);
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
            .WithMessageLike(Resources.ValidationExtensions_InvalidDatabaseResourceName);
    }

    [Fact]
    public async Task WhenDestroyAllWithInvalidContainerName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.BlobStore
            .Invoking(x => x.DestroyAllAsync("^aninvalidcontainername^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidDatabaseResourceName);
#endif
    }
}