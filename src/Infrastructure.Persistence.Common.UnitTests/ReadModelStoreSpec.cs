using Common;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class ReadModelStoreSpec
{
    private readonly Mock<IDataStore> _dataStore;
    private readonly ReadModelStore<TestReadModel> _store;

    public ReadModelStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        var domainFactory = new Mock<IDomainFactory>();
        _dataStore = new Mock<IDataStore>();
        _dataStore.Setup(repo =>
                repo.AddAsync(It.IsAny<string>(), It.IsAny<CommandEntity>(), It.IsAny<CancellationToken>()))
            .Returns((string _, CommandEntity entity, CancellationToken _) =>
                Task.FromResult<Result<CommandEntity, Error>>(new CommandEntity(entity.Id)));
        _store =
            new ReadModelStore<TestReadModel>(recorder.Object, domainFactory.Object,
                _dataStore.Object);
    }

    [Fact]
    public async Task WhenCreateAndIdIsEmpty_ThenReturnsError()
    {
        var result = await _store.CreateAsync(string.Empty, _ => { }, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ReadModelStore_NoId);
    }

    [Fact]
    public async Task WhenCreateWithNoAction_ThenCreatesAndReturnsDto()
    {
        var result = await _store.CreateAsync("anid", null, CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(repo => repo.AddAsync("acontainername", It.Is<CommandEntity>(entity =>
            entity.Id == "anid"
        ), CancellationToken.None));
    }

    [Fact]
    public async Task WhenCreateWithInitialisingAction_ThenCreatesAndReturnsDto()
    {
        var result =
            await _store.CreateAsync("anid", entity => entity.AStringValue = "avalue",
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(repo => repo.AddAsync("acontainername", It.Is<CommandEntity>(entity =>
            entity.Id == "anid"
            && entity.Properties[nameof(TestReadModel.AStringValue)].ToString() == "avalue"
        ), CancellationToken.None));
    }

    [Fact]
    public async Task WhenDeleteAndIdIsEmpty_ThenReturnsError()
    {
        var result = await _store.DeleteAsync(string.Empty, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation, Resources.ReadModelStore_NoId);
    }

    [Fact]
    public async Task WhenDeleteAndNotExists_ThenReturnsError()
    {
        _dataStore.Setup(repo =>
                repo.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None));

        var result = await _store.DeleteAsync("anid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
    }

    [Fact]
    public async Task WhenDelete_ThenDeletes()
    {
        _dataStore.Setup(repo =>
                repo.RetrieveAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid").ToOptional()));

        await _store.DeleteAsync("anid", CancellationToken.None);

        _dataStore.Verify(repo => repo.RemoveAsync("acontainername", "anid", CancellationToken.None));
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
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None));

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
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None));

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
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional()));

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
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional()));

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
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional()));

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
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid").ToOptional()));

        var result = await _store.GetAsync("anid", true, false, CancellationToken.None);

        result.Value.Value.Id.Should().BeSome("anid");
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenQueryWithEmpty_ThenReturnsEmptyResults()
    {
        var query = Query.Empty<TestReadModel>();

        var result = await _store.QueryAsync(query, false, CancellationToken.None);

        result.Value.Results.Should().BeEmpty();
        _dataStore.Verify(
            store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestReadModel>>(),
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenQuery_ThenReturnsEmptyResults()
    {
        var query = Query.From<TestReadModel>().WhereAll();
        var entity = new QueryEntity();
        entity.Add(nameof(TestReadModel.AStringValue), "avalue");
        _dataStore.Setup(
                store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestReadModel>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<List<QueryEntity>, Error>>(new List<QueryEntity>
            {
                entity
            }));

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
        var query = Query.From<TestReadModel>().WhereAll();
        var entity = new QueryEntity
        {
            IsDeleted = true
        };
        entity.Add(nameof(TestReadModel.AStringValue), "avalue");
        _dataStore.Setup(
                store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestReadModel>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<List<QueryEntity>, Error>>(new List<QueryEntity>
            {
                entity
            }));

        var result = await _store.QueryAsync(query, false, CancellationToken.None);

        result.Value.Results.Count.Should().Be(0);
        _dataStore.Verify(
            store => store.QueryAsync("acontainername", query, It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenQueryAndResultIsSoftDeletedAndIncludeDeleted_ThenReturnsResult()
    {
        var query = Query.From<TestReadModel>().WhereAll();
        var entity = new QueryEntity
        {
            IsDeleted = true
        };
        entity.Add(nameof(TestReadModel.AStringValue), "avalue");
        _dataStore.Setup(
                store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestReadModel>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<List<QueryEntity>, Error>>(new List<QueryEntity>
            {
                entity
            }));

        var result = await _store.QueryAsync(query, true, CancellationToken.None);

        result.Value.Results.Count.Should().Be(1);
        result.Value.Results[0].AStringValue.Should().Be("avalue");
        _dataStore.Verify(
            store => store.QueryAsync("acontainername", query, It.IsAny<PersistedEntityMetadata>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task WhenUpdateAndIdIsEmpty_ThenReturnsError()
    {
        var result = await _store.UpdateAsync(string.Empty, _ => { }, CancellationToken.None);

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
            await _store.UpdateAsync("anid", entity => entity.AStringValue = "avalue",
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
            await _store.UpdateAsync("anid", entity => entity.AStringValue = "avalue",
                CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(repo => repo.ReplaceAsync("acontainername", "anid", It.Is<CommandEntity>(
            entity =>
                entity.Id == "anid"
                && entity.Properties[nameof(TestReadModel.AStringValue)].ToString() == "avalue"
        ), CancellationToken.None));
    }

    [Fact]
    public async Task WhenUpsertAndDtoIdIsMissing_ThenReturnsError()
    {
        var result = await _store.UpsertAsync(new TestReadModel(), false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.ReadModelStore_MissingIdentifier);
    }

    [Fact]
    public async Task WhenUpsertAndEntityNotExists_ThenAddsToStore()
    {
        var dto = new TestReadModel { Id = "anid" };
        var addedEntity = new CommandEntity("anid");
        _dataStore.Setup(store => store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None));
        _dataStore.Setup(store =>
                store.AddAsync(It.IsAny<string>(), It.IsAny<CommandEntity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<CommandEntity, Error>>(addedEntity));

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
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional()));

        var result = await _store.UpsertAsync(new TestReadModel { Id = "anid" }, false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.ReadModelStore_DtoDeleted);
    }

    [Fact]
    public async Task WhenUpsertAndSoftDeletedWithIncludeDeleted_ThenResurrectsAndReplacesInStore()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional()));
        _dataStore.Setup(store =>
                store.ReplaceAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CommandEntity>(),
                    It.IsAny<CancellationToken>()))
            .Returns((string _, string _, CommandEntity entity, CancellationToken _) =>
                Task.FromResult<Result<Optional<CommandEntity>, Error>>(entity.ToOptional()));

        var result = await _store.UpsertAsync(new TestReadModel
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
        var dto = new TestReadModel { Id = "anupsertedid", AStringValue = "anewvalue" };
        var fetchedEntity = new CommandEntity("anid");
        var updatedEntity = new CommandEntity("anid");
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", It.IsAny<string>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(fetchedEntity.ToOptional()));
        _dataStore.Setup(store =>
                store.ReplaceAsync("acontainername", It.IsAny<string>(), It.IsAny<CommandEntity>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(updatedEntity.ToOptional()));

        var result = await _store.UpsertAsync(dto, false, CancellationToken.None);

        result.Value.Id.Should().Be("anid");
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anupsertedid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
        _dataStore.Verify(store =>
            store.ReplaceAsync("acontainername", "anupsertedid", It.IsAny<CommandEntity>(), CancellationToken.None));
    }
}