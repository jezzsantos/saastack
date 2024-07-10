using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class SnapshottingDddCommandStoreSpec
{
    private readonly Mock<IDataStore> _dataStore;
    private readonly Mock<IDomainFactory> _domainFactory;

    private readonly SnapshottingDddCommandStore<TestCommandEntity> _store;

    public SnapshottingDddCommandStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        _domainFactory = new Mock<IDomainFactory>();
        _domainFactory.Setup(df =>
                df.RehydrateEntity(typeof(TestCommandEntity),
                    It.IsAny<HydrationProperties>()))
            .Returns((Type _, HydrationProperties props) =>
                new TestCommandEntity(props[nameof(TestCommandEntity.Id)].ValueOrDefault!.ToString()!)
                {
                    IsDeleted = props[nameof(TestCommandEntity.IsDeleted)].ValueOrDefault.As<bool>().ToOptional()
                });
        _domainFactory.Setup(df =>
                df.RehydrateValueObject(typeof(ISingleValueObject<string>), It.IsAny<string>()))
            .Returns((Type _, string value) => value.ToId());
        _dataStore = new Mock<IDataStore>();
        _store =
            new SnapshottingDddCommandStore<TestCommandEntity>(recorder.Object, _domainFactory.Object,
                _dataStore.Object);
    }

    [Fact]
    public async Task WhenCount_ThenGetsCountFromStore()
    {
        await _store.CountAsync(CancellationToken.None);

        _dataStore.Verify(store => store.CountAsync("acontainername", CancellationToken.None));
    }

    [Fact]
    public async Task WhenDeleteAndNotExists_ThenReturns()
    {
        _dataStore.Setup(store => store.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        await _store.DeleteAsync("anid".ToId(), false, CancellationToken.None);

        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(
            store => store.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _dataStore.Verify(
            store => store.RemoveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenDeleteAndDestroy_ThenRemovesFromStore()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid").ToOptional());

        await _store.DeleteAsync("anid".ToId());

        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(store => store.RemoveAsync("acontainername", "anid", CancellationToken.None));
    }

    [Fact]
    public async Task WhenDeleteSoftAndAlreadySoftDeleted_ThenReturns()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());

        await _store.DeleteAsync("anid".ToId(), false, CancellationToken.None);

        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(
            store => store.RemoveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenDeleteSoft_ThenReplacesToStore()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid").ToOptional());

        await _store.DeleteAsync("anid".ToId(), false, CancellationToken.None);

        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(store => store.ReplaceAsync("acontainername", "anid", It.Is<CommandEntity>(ce =>
            ce.IsDeleted == true
        ), CancellationToken.None));
    }
#if TESTINGONLY

    [Fact]
    public async Task WhenDestroyAll_ThenDestroysAllInStore()
    {
        await _store.DestroyAllAsync(CancellationToken.None);

        _dataStore.Verify(store => store.DestroyAllAsync("acontainername", CancellationToken.None));
    }
#endif

    [Fact]
    public async Task WhenGetAndNotExistsAndErrorIfNotFound_ThenReturnsError()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<CommandEntity>.None);

        var result = await _store.GetAsync("anid".ToId());

        result.Should().BeError(ErrorCode.EntityNotFound);
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGetAndNotExistsAndNoErrorIfNotFound_ThenReturnsNone()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<CommandEntity>.None);

        var result = await _store.GetAsync("anid".ToId(), false);

        result.Value.Should().BeNone();
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGetAndSoftDeletedAndErrorIfNotFound_ThenReturnsError()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());

        var result = await _store.GetAsync("anid".ToId(), true, false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGetAndSoftDeletedAndNotErrorIfNotFound_ThenReturnsNone()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());

        var result = await _store.GetAsync("anid".ToId(), false, false, CancellationToken.None);

        result.Value.Should().BeNone();
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGetAndSoftDeletedAndIncludeDeleted_ThenRetrievesFromStore()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());

        var result = await _store.GetAsync("anid".ToId(), true, true, CancellationToken.None);

        result.Value.Value.Id.Should().Be("anid".ToId());
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGet_ThenRetrievesFromStore()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid").ToOptional());

        var result = await _store.GetAsync("anid".ToId(), true, false, CancellationToken.None);

        result.Value.Value.Id.Should().Be("anid".ToId());
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenResurrectAndEntityNotExists_ThenReturnsError()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<CommandEntity>.None);

        var result = await _store.ResurrectDeletedAsync("anid".ToId(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(
            store => store.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenResurrectAndNotSoftDeleted_ThenReturnsEntity()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid").ToOptional());

        var result = await _store.ResurrectDeletedAsync("anid".ToId(), CancellationToken.None);

        result.Should().NotBeNull();
        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(
            store => store.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenResurrectAndSoftDeleted_ThenReturnsEntity()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());

        var result = await _store.ResurrectDeletedAsync("anid".ToId(), CancellationToken.None);

        result.Should().NotBeNull();
        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(store => store.ReplaceAsync("acontainername", "anid", It.Is<CommandEntity>(ce =>
            ce.IsDeleted == false), CancellationToken.None));
    }

    [Fact]
    public async Task WhenUpsertAndEntityIdIsMissing_ThenReturnsError()
    {
        var result =
            await _store.UpsertAsync(new TestCommandEntity(Identifier.Empty()), false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound,
            Resources.SnapshottingDddCommandStore_EntityMissingIdentifier);
    }

    [Fact]
    public async Task WhenUpsertAndEntityNotExists_ThenAddsToStore()
    {
        var entity = new TestCommandEntity("anid".ToId());
        var addedEntity = new CommandEntity("anid");
        _dataStore.Setup(store => store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<CommandEntity>.None);
        _dataStore.Setup(store =>
                store.AddAsync(It.IsAny<string>(), It.IsAny<CommandEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addedEntity);

        await _store.UpsertAsync(entity, false, CancellationToken.None);

        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
        _dataStore.Verify(store =>
            store.AddAsync("acontainername", It.IsAny<CommandEntity>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenUpsertAndSoftDeleted_ThenReturnsError()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());

        var result =
            await _store.UpsertAsync(new TestCommandEntity("anid".ToId()), false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.SnapshottingDddCommandStore_EntityDeleted);
    }

    [Fact]
    public async Task WhenUpsertAndSoftDeletedWithIncludeDeleted_ThenResurrectsAndReplacesInStore()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());
        _dataStore.Setup(store =>
                store.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                    It.IsAny<CancellationToken>()))
            .Returns((string _, string _, CommandEntity entity, CancellationToken _)
                => Task.FromResult<Result<Optional<CommandEntity>, Error>>(entity.ToOptional()));

        var result = await _store.UpsertAsync(new TestCommandEntity("anid".ToId())
        {
            AStringValue = "astringvalue",
            IsDeleted = true
        }, true, CancellationToken.None);

        result.Value.Id.Should().Be("anid".ToId());
        result.Value.IsDeleted.Should().BeSome(false);
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
        _dataStore.Verify(store =>
            store.ReplaceAsync("acontainername", "anid", It.Is<CommandEntity>(entity =>
                entity.IsDeleted == false
            ), CancellationToken.None));
    }

    [Fact]
    public async Task WhenUpsertAndEntityExists_ThenReplacesInStore()
    {
        var entity = new TestCommandEntity("anupsertedid".ToId()) { AStringValue = "anewvalue" };
        var fetchedEntity = new CommandEntity("anid");
        var updatedEntity = new CommandEntity("anid");
        var hydratedEntity = new TestCommandEntity("anid".ToId());
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", It.IsAny<string>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchedEntity.ToOptional());
        _dataStore.Setup(store =>
                store.ReplaceAsync("acontainername", It.IsAny<string>(), It.IsAny<CommandEntity>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedEntity.ToOptional());
        _domainFactory.Setup(df =>
                df.RehydrateEntity(It.IsAny<Type>(),
                    It.IsAny<HydrationProperties>()))
            .Returns(hydratedEntity);

        var result = await _store.UpsertAsync(entity, false, CancellationToken.None);

        result.Value.Should().BeEquivalentTo(hydratedEntity);
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anupsertedid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
        _dataStore.Verify(store =>
            store.ReplaceAsync("acontainername", "anupsertedid", It.IsAny<CommandEntity>(), CancellationToken.None));
    }
}