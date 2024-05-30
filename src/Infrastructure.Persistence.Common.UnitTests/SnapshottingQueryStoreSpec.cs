using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class SnapshottingQueryStoreSpec
{
    private readonly Mock<IDataStore> _dataStore;
    private readonly SnapshottingQueryStore<TestQueryEntity> _store;

    public SnapshottingQueryStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        var domainFactory = new Mock<IDomainFactory>();
        _dataStore = new Mock<IDataStore>();
        _store =
            new SnapshottingQueryStore<TestQueryEntity>(recorder.Object, domainFactory.Object,
                _dataStore.Object);
    }

    [Fact]
    public async Task WhenCount_ThenGetsCountFromRepo()
    {
        await _store.CountAsync(CancellationToken.None);

        _dataStore.Verify(store => store.CountAsync("acontainername", CancellationToken.None));
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
    public async Task WhenQueryWithEmptyQuery_ThenReturnsEmptyResults()
    {
        var result = await _store.QueryAsync(Query.Empty<TestQueryEntity>(), false, CancellationToken.None);

        result.Should().NotBeNull();
        result.Value.Results.Should().BeEmpty();
        _dataStore.Verify(
            store => store.QueryAsync(It.IsAny<string>(), It.IsAny<QueryClause<TestQueryEntity>>(),
                It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenQueryAndDeleted_ThenReturnsNonDeletedResults()
    {
        var query = Query.From<TestQueryEntity>().WhereAll();
        var results = new List<QueryEntity>
        {
            new() { Id = "anid1", IsDeleted = true },
            new() { Id = "anid2", IsDeleted = false },
            new() { Id = "anid3", IsDeleted = Optional<bool>.None }
        };
        _dataStore.Setup(store =>
                store.QueryAsync("acontainername", It.IsAny<QueryClause<TestQueryEntity>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<List<QueryEntity>, Error>>(results));

        var result = await _store.QueryAsync(query, false, CancellationToken.None);

        result.Value.Results.Count.Should().Be(2);
        result.Value.Results[0].Id.Should().Be("anid2");
        result.Value.Results[1].Id.Should().Be("anid3");
    }

    [Fact]
    public async Task WhenQueryAndDeletedAndIncludeDeleted_ThenReturnsDeletedResults()
    {
        var query = Query.From<TestQueryEntity>().WhereAll();
        var results = new List<QueryEntity>
        {
            new() { Id = "anid1", IsDeleted = true },
            new() { Id = "anid2", IsDeleted = false },
            new() { Id = "anid3", IsDeleted = Optional<bool>.None }
        };
        _dataStore.Setup(store =>
                store.QueryAsync("acontainername", It.IsAny<QueryClause<TestQueryEntity>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<List<QueryEntity>, Error>>(results));

        var result = await _store.QueryAsync(query, true, CancellationToken.None);

        result.Value.Results.Count.Should().Be(3);
        result.Value.Results[0].Id.Should().Be("anid1");
        result.Value.Results[1].Id.Should().Be("anid2");
        result.Value.Results[2].Id.Should().Be("anid3");
    }

    [Fact]
    public async Task WhenQuery_ThenReturnsAllResults()
    {
        var query = Query.From<TestQueryEntity>().WhereAll();
        var results = new List<QueryEntity>();
        _dataStore.Setup(store =>
                store.QueryAsync("acontainername", It.IsAny<QueryClause<TestQueryEntity>>(),
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<List<QueryEntity>, Error>>(results));

        var result = await _store.QueryAsync(query, false, CancellationToken.None);

        result.Value.Results.Should().BeEquivalentTo(results);
    }

    [Fact]
    public async Task WhenGetAndNotExistsAndErrorIfNotFound_ThenReturnsError()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid",
                    It.IsAny<PersistedEntityMetadata>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(Optional<CommandEntity>.None));

        var result = await _store.GetAsync<TestQueryEntity>("anid".ToId(), true, false, CancellationToken.None);

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

        var result = await _store.GetAsync<TestQueryEntity>("anid".ToId(), false, false, CancellationToken.None);

        result.Value.Should().BeNone();
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGetAndSoftDeletedAndErrorIfNotFound_ThenReturnsError()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional()));

        var result = await _store.GetAsync<TestQueryEntity>("anid".ToId(), true, false, CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGetAndSoftDeletedAndNotErrorIfNotFound_ThenReturnsNone()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid")
            {
                IsDeleted = true
            }.ToOptional()));

        var result = await _store.GetAsync<TestQueryEntity>("anid".ToId(), false, false, CancellationToken.None);

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

        var result = await _store.GetAsync<TestQueryEntity>("anid".ToId(), true, true, CancellationToken.None);

        result.Value.Value.Id.Should().BeSome("anid");
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }

    [Fact]
    public async Task WhenGet_ThenRetrievesFromStore()
    {
        _dataStore.Setup(store =>
                store.RetrieveAsync("acontainername", "anid", It.IsAny<PersistedEntityMetadata>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<CommandEntity>, Error>>(new CommandEntity("anid").ToOptional()));

        var result = await _store.GetAsync<TestQueryEntity>("anid".ToId(), true, false, CancellationToken.None);

        result.Value.Value.Id.Should().BeSome("anid");
        _dataStore.Verify(store =>
            store.RetrieveAsync("acontainername", "anid",
                It.IsAny<PersistedEntityMetadata>(), CancellationToken.None));
    }
}