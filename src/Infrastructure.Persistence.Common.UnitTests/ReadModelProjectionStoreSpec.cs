using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class ReadModelProjectionStoreSpec
{
    private readonly Mock<IDataStore> _dataStore;
    private readonly ReadModelProjectionStore<TestReadModel> _projectionStore;

    public ReadModelProjectionStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        var domainFactory = new Mock<IDomainFactory>();
        _dataStore = new Mock<IDataStore>();
        _dataStore.Setup(repo =>
                repo.AddAsync(It.IsAny<string>(), It.IsAny<CommandEntity>(), It.IsAny<CancellationToken>()))
            .Returns((string _, CommandEntity entity, CancellationToken _) =>
                Task.FromResult<Result<CommandEntity, Error>>(new CommandEntity(entity.Id)));
        _projectionStore =
            new ReadModelProjectionStore<TestReadModel>(recorder.Object, domainFactory.Object,
                _dataStore.Object);
    }

    [Fact]
    public async Task WhenCreateAndIdIsEmpty_ThenReturnsError()
    {
        var result = await _projectionStore.CreateAsync(string.Empty, _ => { }, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ReadModelStore_NoId);
    }

    [Fact]
    public async Task WhenCreateWithNoAction_ThenCreatesAndReturnsDto()
    {
        var result = await _projectionStore.CreateAsync("anid", null, CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(repo => repo.AddAsync("acontainername", It.Is<CommandEntity>(entity =>
            entity.Id == "anid"
        ), CancellationToken.None));
    }

    [Fact]
    public async Task WhenCreateWithInitialisingAction_ThenCreatesAndReturnsDto()
    {
        var result =
            await _projectionStore.CreateAsync("anid", entity => entity.APropertyName = "avalue",
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(repo => repo.AddAsync("acontainername", It.Is<CommandEntity>(entity =>
            entity.Id == "anid"
            && entity.Properties[nameof(TestReadModel.APropertyName)].ToString() == "avalue"
        ), CancellationToken.None));
    }

    [Fact]
    public async Task WhenDeleteAndIdIsEmpty_ThenReturnsError()
    {
        var result = await _projectionStore.DeleteAsync(string.Empty, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ReadModelStore_NoId);
    }

    [Fact]
    public async Task WhenDeleteAndNotExists_ThenReturnsError()
    {
        _dataStore.Setup(repo =>
                repo.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None));

        var result = await _projectionStore.DeleteAsync("anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDelete_ThenDeletes()
    {
        _dataStore.Setup(repo =>
                repo.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid").ToOptional()));

        await _projectionStore.DeleteAsync("anid", CancellationToken.None);

        _dataStore.Verify(repo => repo.RemoveAsync("acontainername", "anid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenUpdateAndIdIsEmpty_ThenReturnsError()
    {
        var result = await _projectionStore.UpdateAsync(string.Empty, _ => { }, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ReadModelStore_NoId);
    }

    [Fact]
    public async Task WhenUpdateAndNotExists_ThenReturnsError()
    {
        _dataStore.Setup(repo =>
                repo.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None));

        var result =
            await _projectionStore.UpdateAsync("anid", entity => entity.APropertyName = "avalue",
                CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenUpdate_ThenUpdatesAndReturnsDto()
    {
        _dataStore.Setup(repo =>
                repo.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid").ToOptional()));
        _dataStore.Setup(repo =>
                repo.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid").ToOptional()));

        var result =
            await _projectionStore.UpdateAsync("anid", entity => entity.APropertyName = "avalue",
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(repo => repo.ReplaceAsync("acontainername", "anid", It.Is<CommandEntity>(
            entity =>
                entity.Id == "anid"
                && entity.Properties[nameof(TestReadModel.APropertyName)].ToString() == "avalue"
        ), CancellationToken.None));
    }
}