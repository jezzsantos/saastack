using Common;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class SnapshottingStoreSpec
{
    private readonly Mock<IDataStore> _dataStore;
    private readonly SnapshottingStore<TestDto> _store;

    public SnapshottingStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        _dataStore = new Mock<IDataStore>();
        _store = new SnapshottingStore<TestDto>(recorder.Object, _dataStore.Object);
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

        await _store.DeleteAsync("anid", false, CancellationToken.None);

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

        await _store.DeleteAsync("anid");

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

        await _store.DeleteAsync("anid", false, CancellationToken.None);

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

        await _store.DeleteAsync("anid", false, CancellationToken.None);

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

        var result = await _store.GetAsync("anid");

        result.Should().BeError(ErrorCode.EntityNotFound);
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGetAndNotExistsAndNotErrorIfNotFound_ThenReturnsNone()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<CommandEntity>.None);

        var result = await _store.GetAsync("anid", false);

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

        var result = await _store.GetAsync("anid", true, false, CancellationToken.None);

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

        var result = await _store.GetAsync("anid", false, false, CancellationToken.None);

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

        var result = await _store.GetAsync("anid", true, true, CancellationToken.None);

        result.Value.Value.Id.Should().BeSome("anid");
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

        var result = await _store.GetAsync("anid", true, false, CancellationToken.None);

        result.Value.Value.Id.Should().BeSome("anid");
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenQueryWithEmpty_ThenReturnsEmptyResults()
    {
        var query = Query.Empty<TestDto>();

        var result = await _store.QueryAsync(query, false, CancellationToken.None);

        result.Value.Results.Should().BeEmpty();
        _dataStore.Verify(
            store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestDto>>(),
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenQuery_ThenReturnsEmptyResults()
    {
        var query = Query.From<TestDto>().WhereAll();
        var entity = new QueryEntity();
        entity.Add(nameof(TestDto.AStringValue), "avalue");
        _dataStore.Setup(
                store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestDto>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueryEntity>
            {
                entity
            });

        var result = await _store.QueryAsync(query, false, CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].AStringValue.Should().Be("avalue");
        _dataStore.Verify(
            store => store.QueryAsync("acontainername", query, It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenQueryAndResultIsSoftDeleted_ThenReturnsEmptyResults()
    {
        var query = Query.From<TestDto>().WhereAll();
        var entity = new QueryEntity
        {
            IsDeleted = true
        };
        entity.Add(nameof(TestDto.AStringValue), "avalue");
        _dataStore.Setup(
                store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestDto>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueryEntity>
            {
                entity
            });

        var result = await _store.QueryAsync(query, false, CancellationToken.None);

        result.Value.Results.Count.Should().Be(0);
        _dataStore.Verify(
            store => store.QueryAsync("acontainername", query, It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenQueryAndResultIsSoftDeletedAndIncludeDeleted_ThenReturnsResult()
    {
        var query = Query.From<TestDto>().WhereAll();
        var entity = new QueryEntity
        {
            IsDeleted = true
        };
        entity.Add(nameof(TestDto.AStringValue), "avalue");
        _dataStore.Setup(
                store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestDto>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<QueryEntity>
            {
                entity
            });

        var result = await _store.QueryAsync(query, true, CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].AStringValue.Should().Be("avalue");
        _dataStore.Verify(
            store => store.QueryAsync("acontainername", query, It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenResurrectAndEntityNotExists_ThenReturnsError()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<CommandEntity>.None);

        var result = await _store.ResurrectDeletedAsync("anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(
            store => store.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenResurrectAndNotSoftDeleted_ThenReturnsDto()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid").ToOptional());

        var result = await _store.ResurrectDeletedAsync("anid", CancellationToken.None);

        result.Should().NotBeNull();
        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(
            store => store.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenResurrectAndSoftDeleted_ThenReturnsDto()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional());

        var result = await _store.ResurrectDeletedAsync("anid", CancellationToken.None);

        result.Should().NotBeNull();
        _dataStore.Verify(
            store => store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
        _dataStore.Verify(store => store.ReplaceAsync("acontainername", "anid", It.Is<CommandEntity>(ce =>
            ce.IsDeleted == false), CancellationToken.None));
    }

    [Fact]
    public async Task WhenUpsertAndDtoIdIsMissing_ThenReturnsError()
    {
        var result = await _store.UpsertAsync(new TestDto(), false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.SnapshottingStore_DtoMissingIdentifier);
    }

    [Fact]
    public async Task WhenUpsertAndEntityNotExists_ThenAddsToStore()
    {
        var dto = new TestDto { Id = "anid" };
        var addedEntity = new CommandEntity("anid");
        _dataStore.Setup(store => store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<CommandEntity>.None);
        _dataStore.Setup(store =>
                store.AddAsync(It.IsAny<string>(), It.IsAny<CommandEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addedEntity);

        await _store.UpsertAsync(dto, false, CancellationToken.None);

        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
        _dataStore.Verify(store => store.AddAsync("acontainername", It.IsAny<CommandEntity>(), CancellationToken.None));
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

        var result = await _store.UpsertAsync(new TestDto { Id = "anid" }, false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.SnapshottingStore_DtoDeleted);
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
            .Returns((string _, string _, CommandEntity entity, CancellationToken _) =>
                Task.FromResult<Result<Optional<CommandEntity>, Error>>(entity.ToOptional()));

        var result = await _store.UpsertAsync(new TestDto
        {
            Id = "anid",
            AStringValue = "astringvalue",
            IsDeleted = true
        }, true, CancellationToken.None);

        result.Value.IsDeleted.Should().BeSome(false);
        result.Value.Id.Should().BeSome("anid");
    }

    [Fact]
    public async Task WhenUpsertAndEntityExists_ThenReplacesInStore()
    {
        var dto = new TestDto { Id = "anupsertedid", AStringValue = "anewvalue" };
        var fetchedEntity = new CommandEntity("anid");
        var updatedEntity = new CommandEntity("anid");
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", It.IsAny<string>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchedEntity.ToOptional());
        _dataStore.Setup(store =>
                store.ReplaceAsync("acontainername", It.IsAny<string>(), It.IsAny<CommandEntity>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedEntity.ToOptional());

        var result = await _store.UpsertAsync(dto, false, CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anupsertedid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
        _dataStore.Verify(store =>
            store.ReplaceAsync("acontainername", "anupsertedid", It.IsAny<CommandEntity>(), CancellationToken.None));
    }
}