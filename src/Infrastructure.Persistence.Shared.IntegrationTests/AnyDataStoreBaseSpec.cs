using Common;
using Common.Recording;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Common;
using Infrastructure.Persistence.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

public abstract class AnyDataStoreBaseSpec
{
    private const int ReasonableNumberOfEntitiesToSort = 50; //number of entities to create to thoroughly test sorting
    private static readonly IDomainFactory DomainFactory;
    private static readonly TimeSpan ReasonableTimeDelayBetweenTimestamps = TimeSpan.FromMilliseconds(20);
    private static readonly IRecorder Recorder = NullRecorder.Instance;
    private readonly DataStoreInfo _firstJoiningSetup;
    private readonly DataStoreInfo _secondJoiningSetup;
    protected readonly DataStoreInfo Setup;

    static AnyDataStoreBaseSpec()
    {
        var container = new ServiceCollection();
        container.AddSingleton(Recorder);
        DomainFactory = Infrastructure.Common.DomainFactory.CreateRegistered(new DotNetDependencyContainer(container),
            typeof(TestDataStoreEntity).Assembly);
    }

    protected AnyDataStoreBaseSpec(IDataStore dataStore)
    {
        Setup = new DataStoreInfo
            { Store = dataStore, ContainerName = typeof(TestDataStoreEntity).GetEntityNameSafe() };
        Setup.Store.DestroyAllAsync(Setup.ContainerName, CancellationToken.None).GetAwaiter()
            .GetResult();
        _firstJoiningSetup = new DataStoreInfo
        {
            Store = dataStore, ContainerName = typeof(FirstJoiningTestQueryStoreEntity).GetEntityNameSafe()
        };
        _firstJoiningSetup.Store
            .DestroyAllAsync(_firstJoiningSetup.ContainerName, CancellationToken.None).GetAwaiter().GetResult();
        _secondJoiningSetup = new DataStoreInfo
        {
            Store = dataStore, ContainerName = typeof(SecondJoiningTestQueryStoreEntity).GetEntityNameSafe()
        };
        _secondJoiningSetup.Store
            .DestroyAllAsync(_secondJoiningSetup.ContainerName, CancellationToken.None).GetAwaiter()
            .GetResult();
    }

    [Fact]
    public async Task WhenAddWithNullEntity_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.AddAsync(Setup.ContainerName, null!, CancellationToken.None)).Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenAddWithNullContainer_ThenThrows()
    {
        var entity = new CommandEntity("anid");
        await Setup.Store
            .Invoking(x => x.AddAsync(null!, entity, CancellationToken.None)).Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenAdd_ThenAddsEntity()
    {
        var entity = new CommandEntity("anid");
        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);

        var count = await Setup.Store.CountAsync(Setup.ContainerName, CancellationToken.None);

        count.Should().Be(1);
    }

    [Fact]
    public async Task WhenCountWithNullContainer_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.CountAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenCountAndEmpty_ThenReturnsZero()
    {
        var count = await Setup.Store.CountAsync(Setup.ContainerName, CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task WhenCountAndNotEmpty_ThenReturnsCount()
    {
        await Setup.Store.AddAsync(Setup.ContainerName, new CommandEntity("anid1"),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName, new CommandEntity("anid2"),
            CancellationToken.None);

        var count = await Setup.Store.CountAsync(Setup.ContainerName, CancellationToken.None);

        count.Should().Be(2);
    }

    [Fact]
    public async Task WhenDestroyAllWithNullContainer_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.DestroyAllAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenQueryAndQueryIsNull_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.QueryAsync<TestDataStoreEntity>(Setup.ContainerName, null!,
                PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenQueryAndEmpty_ThenReturnsEmptyResults()
    {
        var query = Query.Empty<TestDataStoreEntity>();
        await Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(new TestDataStoreEntity
        {
            AStringValue = "avalue"
        }), CancellationToken.None);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryAndWhereAll_ThenReturnsAllResults()
    {
        var query = Query.From<TestDataStoreEntity>()
            .WhereAll();
        var entity = Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(
            new TestDataStoreEntity
            {
                AStringValue = "avalue"
            }), CancellationToken.None);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity.Result.Value.Id);
    }

    [Fact]
    public async Task WhenQueryAndNoEntities_ThenReturnsEmptyResults()
    {
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.EqualTo, "avalue");

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryAndNoMatch_ThenReturnsEmptyResults()
    {
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.EqualTo, "anothervalue");
        await Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(new TestDataStoreEntity
        {
            AStringValue = "avalue"
        }), CancellationToken.None);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryAndMatchOne_ThenReturnsResult()
    {
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.EqualTo, "avalue");
        var entity = CommandEntity.FromType(new TestDataStoreEntity
        {
            AStringValue = "avalue"
        });
        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity.Id);
    }

    [Fact]
    public async Task WhenQueryAndMatchMany_ThenReturnsResults()
    {
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.EqualTo, "avalue");
        var entity1 = Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(
            new TestDataStoreEntity
            {
                AStringValue = "avalue"
            }), CancellationToken.None);
        var entity2 = Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(
            new TestDataStoreEntity
            {
                AStringValue = "avalue"
            }), CancellationToken.None);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Result.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Result.Value.Id);
    }

    [Fact]
    public async Task WhenQueryWithId_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var entity2 = CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" });
        await Setup.Store.AddAsync(Setup.ContainerName, entity2, CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where<string>(e => e.Id, ConditionOperator.EqualTo, entity2.Id.Value);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Id);
    }

    [Fact]
    public async Task WhenQueryForStringValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.EqualTo, "avalue2");

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForStringValueContainingSingleQuote_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalu'e" }), CancellationToken.None);
        var entity2 = Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "a'value" }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.EqualTo, "a'value");

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Result.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullStringValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = null! }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.AStringValue, ConditionOperator.EqualTo, null);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForStringValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = null! }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.EqualTo, "astring");

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForNotNullStringValue_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = null! }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>().Where(e => e.AStringValue, ConditionOperator.NotEqualTo, null);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public virtual async Task WhenQueryForStringValueWithLikeExact_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue" }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue" }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.Like, "avalue");

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public virtual async Task WhenQueryForStringValueWithLikePartial_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.Like, "value");

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public virtual async Task WhenQueryForStringValueWithLikeIncludingPercentSign_ThenReturnsResult()
    {
        var entity1 = Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "av%alue" }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AStringValue, ConditionOperator.Like, "%al");

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Result.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForAnEnumValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { EnumValue = TestEnum.AValue1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { EnumValue = TestEnum.AValue2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.EnumValue, ConditionOperator.EqualTo, TestEnum.AValue2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForANullableAnEnumValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AnNullableEnumValue = TestEnum.AValue1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AnNullableEnumValue = null }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AnNullableEnumValue, ConditionOperator.EqualTo, null);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeUtcValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = null }),
            CancellationToken.None);

        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.EqualTo, DateTime.UtcNow);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForDateTimeUtcValue_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.EqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeUtcValue_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.EqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetValue_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.UtcNow;
        var dateTimeOffset2 = DateTimeOffset.UtcNow.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.EqualTo, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeOffsetValue_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.UtcNow;
        var dateTimeOffset2 = DateTimeOffset.UtcNow.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.EqualTo, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForMinDateTimeValue_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.MinValue;
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.EqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableMinDateTimeValue_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.MinValue;
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.EqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableMinDateTimeOffsetValue_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.MinValue;
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.EqualTo, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeUtcValueGreaterThan_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.GreaterThan, dateTime1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeUtcValueGreaterThanOrEqualTo_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.GreaterThanEqualTo, dateTime1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeUtcValueGreaterThanOrEqualToMinValue_ThenReturnsResult()
    {
        var dateTime1 = DateTime.MinValue;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.GreaterThanEqualTo, DateTime.MinValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeUtcValueLessThan_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.LessThan, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeUtcValueLessThanOrEqual_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = dateTime1.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.LessThanEqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeUtcValueNotEqual_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime1 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeUtcValue = dateTime2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeUtcValue, ConditionOperator.NotEqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeUtcValueGreaterThan_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.GreaterThan, dateTime1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeUtcValueGreaterThanOrEqualTo_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.GreaterThanEqualTo, dateTime1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeUtcValueLessThan_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.LessThan, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeUtcValueLessThanOrEqual_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.LessThanEqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeUtcValueNotEqual_ThenReturnsResult()
    {
        var dateTime1 = DateTime.UtcNow;
        var dateTime2 = DateTime.UtcNow.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeUtcValue = dateTime2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeUtcValue, ConditionOperator.NotEqualTo, dateTime2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetValueGreaterThan_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.GreaterThan, dateTimeOffset1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetValueGreaterThanOrEqualTo_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.GreaterThanEqualTo, dateTimeOffset1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetValueLessThan_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.LessThan, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetWithEqualTicksButDifferentOffset_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.EqualTo, dateTimeOffset1.ToUniversalTime());

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetValueLessThanOrEqual_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.LessThanEqualTo, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetValueLessThanOrEqualWithEqualTicksButDifferentOffset_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.LessThanEqualTo, dateTimeOffset2.ToUniversalTime());

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForDateTimeOffsetValueNotEqual_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ADateTimeOffsetValue, ConditionOperator.NotEqualTo, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeOffsetValueGreaterThan_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.GreaterThan, dateTimeOffset1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task
        WhenQueryForNullableDateTimeOffsetValueGreaterThanWithEqualTicksButDifferentOffset_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.GreaterThan,
                dateTimeOffset1.ToUniversalTime());

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeOffsetValueGreaterThanOrEqualTo_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.GreaterThanEqualTo, dateTimeOffset1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeOffsetValueLessThan_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.LessThan, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeOffsetValueLessThanOrEqual_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.LessThanEqualTo, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeOffsetValueNotEqual_ThenReturnsResult()
    {
        var dateTimeOffset1 = DateTimeOffset.Now;
        var dateTimeOffset2 = DateTimeOffset.Now.AddDays(1);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = dateTimeOffset2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.NotEqualTo, dateTimeOffset2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDateTimeOffsetValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDateTimeOffsetValue = null }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDateTimeOffsetValue, ConditionOperator.EqualTo, DateTimeOffset.Now);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForBoolValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ABooleanValue = false }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ABooleanValue = true }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.ABooleanValue, ConditionOperator.EqualTo, true);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableBoolValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableBooleanValue = false }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableBooleanValue = true }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableBooleanValue, ConditionOperator.EqualTo, true);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableBoolValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableBooleanValue = null }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableBooleanValue, ConditionOperator.EqualTo, false);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForIntValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.AIntValue, ConditionOperator.EqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableIntValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = null }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.AIntValue, ConditionOperator.EqualTo, 5);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForIntValueGreaterThan_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.AIntValue, ConditionOperator.GreaterThan, 1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForIntValueGreaterThanOrEqualTo_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AIntValue, ConditionOperator.GreaterThanEqualTo, 1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForIntValueLessThan_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 1 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.AIntValue, ConditionOperator.LessThan, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForIntValueLessThanOrEqual_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 2 }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>().Where(e => e.AIntValue, ConditionOperator.LessThanEqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForIntValueNotEqual_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 1 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.AIntValue, ConditionOperator.NotEqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableIntValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 2 }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>().Where(e => e.ANullableIntValue, ConditionOperator.EqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableIntValueGreaterThan_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableIntValue, ConditionOperator.GreaterThan, 1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableIntValueGreaterThanOrEqualTo_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableIntValue, ConditionOperator.GreaterThanEqualTo, 1);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableIntValueLessThan_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 1 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 2 }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>().Where(e => e.ANullableIntValue, ConditionOperator.LessThan, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableIntValueLessThanOrEqual_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableIntValue, ConditionOperator.LessThanEqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(2);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableIntValueNotEqual_ThenReturnsResult()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 1 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableIntValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableIntValue, ConditionOperator.NotEqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForLongValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ALongValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ALongValue = 2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.ALongValue, ConditionOperator.EqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableLongValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableLongValue = 1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableLongValue = 2 }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>().Where(e => e.ANullableLongValue, ConditionOperator.EqualTo, 2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableLongValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableLongValue = null }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>().Where(e => e.ANullableLongValue, ConditionOperator.EqualTo, 5);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForDoubleValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADoubleValue = 1.0 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ADoubleValue = 2.0 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.ADoubleValue, ConditionOperator.EqualTo, 2.0);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDoubleValue_ThenReturnsResult()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDoubleValue = 1.0 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDoubleValue = 2.0 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDoubleValue, ConditionOperator.EqualTo, 2.0);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableDoubleValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableDoubleValue = null }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableDoubleValue, ConditionOperator.EqualTo, 5.0);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForGuidValue_ThenReturnsResult()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AGuidValue = guid1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AGuidValue = guid2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().Where(e => e.AGuidValue, ConditionOperator.EqualTo, guid2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableGuidValue_ThenReturnsResult()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableGuidValue = guid1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableGuidValue = guid2 }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableGuidValue, ConditionOperator.EqualTo, guid2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForNullableGuidValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ANullableGuidValue = null }), CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.ANullableGuidValue, ConditionOperator.EqualTo, Guid.NewGuid());

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForBinaryValue_ThenReturnsResult()
    {
        var binary1 = new byte[] { 0x01 };
        var binary2 = new byte[] { 0x01, 0x02 };
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ABinaryValue = binary1 }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ABinaryValue = binary2 }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>().Where(e => e.ABinaryValue, ConditionOperator.EqualTo, binary2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
    }

    [Fact]
    public async Task WhenQueryForBinaryValueThatIsNotSet_ThenReturnsEmpty()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { ABinaryValue = null! }), CancellationToken.None);
        var query =
            Query.From<TestDataStoreEntity>()
                .Where(e => e.ABinaryValue, ConditionOperator.EqualTo, new byte[] { 0x01 });

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryForComplexNonValueObjectValue_ThenReturnsResult()
    {
        var complex1 = new TestComplexObject { APropertyValue = "avalue1" };
        var complex2 = new TestComplexObject { APropertyValue = "avalue2" };
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AComplexObjectValue = complex1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AComplexObjectValue = complex2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AComplexObjectValue, ConditionOperator.EqualTo, complex2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
        results.Value[0]
            .GetValueOrDefault<TestComplexObject>(nameof(TestDataStoreEntity.AComplexObjectValue))
            .Should().Be(complex2);
    }

    [Fact]
    public async Task WhenQueryForNullComplexNonValueObjectValue_ThenReturnsResult()
    {
        var complex1 = new TestComplexObject { APropertyValue = "avalue1" };
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AComplexObjectValue = complex1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AComplexObjectValue = null! }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AComplexObjectValue, ConditionOperator.EqualTo, null);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
        results.Value[0]
            .GetValueOrDefault<TestComplexObject>(nameof(TestDataStoreEntity.AComplexObjectValue)).Should()
            .BeNull();
    }

    [Fact]
    public async Task WhenQueryForNotEqualNullComplexNonValueObjectValue_ThenReturnsResult()
    {
        var complex1 = new TestComplexObject { APropertyValue = "avalue1" };
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AComplexObjectValue = complex1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AComplexObjectValue = null! }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AComplexObjectValue, ConditionOperator.NotEqualTo, null);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0]
            .GetValueOrDefault<TestComplexObject>(nameof(TestDataStoreEntity.AComplexObjectValue))
            .Should().Be(complex1);
    }

    [Fact]
    public async Task WhenQueryForComplexValueObjectValue_ThenReturnsResult()
    {
        var complex1 = TestValueObject.Create("avalue1", 25, true);
        var complex2 = TestValueObject.Create("avalue2", 50, false);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AValueObjectValue = complex1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AValueObjectValue = complex2 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AValueObjectValue, ConditionOperator.EqualTo, complex2);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
        results.Value[0].GetValueOrDefault<TestValueObject>(nameof(TestDataStoreEntity.AValueObjectValue),
            DomainFactory).Should().BeEquivalentTo(complex2);
    }

    [Fact]
    public async Task WhenQueryForNullComplexValueObjectValue_ThenReturnsResult()
    {
        var complex1 = TestValueObject.Create("avalue1", 25, true);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AValueObjectValue = complex1 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AValueObjectValue = null! }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AValueObjectValue, ConditionOperator.EqualTo, null);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity2.Value.Id);
        results.Value[0].GetValueOrDefault<TestValueObject>(nameof(TestDataStoreEntity.AValueObjectValue),
            DomainFactory).Should().BeNull();
    }

    [Fact]
    public async Task WhenQueryForNotEqualNullComplexValueObjectValue_ThenReturnsResult()
    {
        var complex1 = TestValueObject.Create("avalue1", 25, true);
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AValueObjectValue = complex1 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AValueObjectValue = null! }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Where(e => e.AValueObjectValue, ConditionOperator.NotEqualTo, null);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<TestValueObject>(nameof(TestDataStoreEntity.AValueObjectValue),
            DomainFactory).Should().BeEquivalentTo(complex1);
    }

    [Fact]
    public async Task WhenQueryAndNoSelects_ThenReturnsResultWithAllPropertiesPopulated()
    {
        var entity = CommandEntity.FromType(new TestDataStoreEntity
        {
            ABinaryValue = new byte[] { 0x01 },
            ABooleanValue = true,
            ADoubleValue = 0.1,
            AGuidValue = Guid.Empty,
            AIntValue = 1,
            ALongValue = 2,
            AStringValue = "astringvalue",
            ADateTimeUtcValue = DateTime.Today.ToUniversalTime(),
            ADateTimeOffsetValue = DateTimeOffset.UnixEpoch.ToUniversalTime(),
            AComplexObjectValue =
                new TestComplexObject
                {
                    APropertyValue = "avalue"
                },
            AValueObjectValue = TestValueObject.Create("avalue", 25, true)
        });

        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().WhereAll();

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        var result = results.Value[0];
        result.Id.Should().Be(entity.Id);
        result.GetValueOrDefault<byte[]>(nameof(TestDataStoreEntity.ABinaryValue))!.SequenceEqual(new byte[] { 0x01 })
            .Should().BeTrue();
        result.GetValueOrDefault<bool>(nameof(TestDataStoreEntity.ABooleanValue)).Should().Be(true);
        result.GetValueOrDefault<Guid>(nameof(TestDataStoreEntity.AGuidValue)).Should().Be(Guid.Empty);
        result.GetValueOrDefault<int>(nameof(TestDataStoreEntity.AIntValue)).Should().Be(1);
        result.GetValueOrDefault<long>(nameof(TestDataStoreEntity.ALongValue)).Should().Be(2);
        result.GetValueOrDefault<double>(nameof(TestDataStoreEntity.ADoubleValue)).Should().Be(0.1);
        result.GetValueOrDefault<string>(nameof(TestDataStoreEntity.AStringValue)).Should().Be("astringvalue");
        result.GetValueOrDefault<DateTime>(nameof(TestDataStoreEntity.ADateTimeUtcValue)).Should()
            .Be(DateTime.Today.ToUniversalTime());
        result.GetValueOrDefault<DateTimeOffset>(nameof(TestDataStoreEntity.ADateTimeOffsetValue)).Should()
            .Be(DateTimeOffset.UnixEpoch.ToUniversalTime());
        result.GetValueOrDefault<TestComplexObject>(nameof(TestDataStoreEntity.AComplexObjectValue))
            .Should().Be(new TestComplexObject { APropertyValue = "avalue" });
        result.GetValueOrDefault<TestValueObject>(nameof(TestDataStoreEntity.AValueObjectValue),
            DomainFactory).Should().Be(TestValueObject.Create("avalue", 25, true));
    }

    [Fact]
    public async Task WhenQueryAndSelect_ThenReturnsResultWithOnlySelectedPropertiesPopulated()
    {
        var entity = CommandEntity.FromType(new TestDataStoreEntity
        {
            ABinaryValue = new byte[] { 0x01 },
            ABooleanValue = true,
            ADoubleValue = 0.1,
            AGuidValue = Guid.Empty,
            AIntValue = 1,
            ALongValue = 2,
            AStringValue = "astringvalue",
            ADateTimeUtcValue = DateTime.Today.ToUniversalTime(),
            ADateTimeOffsetValue = DateTimeOffset.UnixEpoch.ToUniversalTime(),
            AComplexObjectValue =
                new TestComplexObject
                {
                    APropertyValue = "avalue"
                },
            AValueObjectValue = TestValueObject.Create("avalue", 25, true)
        });

        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>().WhereAll()
            .Select(e => e.ABinaryValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        var result = results.Value[0];
        result.Id.Should().Be(entity.Id);
        result.GetValueOrDefault<byte[]>(nameof(TestDataStoreEntity.ABinaryValue))!.SequenceEqual(new byte[] { 0x01 })
            .Should().BeTrue();
        result.GetValueOrDefault<bool>(nameof(TestDataStoreEntity.ABooleanValue)).Should().Be(false);
        result.GetValueOrDefault<Guid>(nameof(TestDataStoreEntity.AGuidValue)).Should().Be(Guid.Empty);
        result.GetValueOrDefault<int>(nameof(TestDataStoreEntity.AIntValue)).Should().Be(0);
        result.GetValueOrDefault<long>(nameof(TestDataStoreEntity.ALongValue)).Should().Be(0);
        result.GetValueOrDefault<double>(nameof(TestDataStoreEntity.ADoubleValue)).Should().Be(0);
        result.GetValueOrDefault<string>(nameof(TestDataStoreEntity.AStringValue)).Should().BeNull();
        result.GetValueOrDefault<DateTime>(nameof(TestDataStoreEntity.ADateTimeUtcValue)).Should()
            .Be(DateTime.MinValue);
        result.GetValueOrDefault<DateTimeOffset>(nameof(TestDataStoreEntity.ADateTimeOffsetValue)).Should()
            .Be(DateTimeOffset.MinValue);
        result.GetValueOrDefault<TestComplexObject>(nameof(TestDataStoreEntity.AComplexObjectValue))
            .Should().BeNull();
        result.GetValueOrDefault<TestValueObject>(nameof(TestDataStoreEntity.AValueObjectValue),
            DomainFactory).Should().BeNull();
    }

    [Fact]
    public async Task WhenQueryWithInnerJoinAndOtherCollectionNotExists_ThenReturnsNoResults()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .WhereAll();

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryWithInnerJoinOnOtherCollection_ThenReturnsOnlyMatchedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue1" }),
            CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue3" }),
            CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .WhereAll();

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryWithLeftJoinAndOtherCollectionNotExists_ThenReturnsAllPrimaryResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue, JoinType.Left)
            .WhereAll();

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
    }

    [Fact]
    public async Task WhenQueryWithLeftJoinOnOtherCollection_ThenReturnsAllPrimaryResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        var entity3 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue3" }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue1" }),
            CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue5" }),
            CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue, JoinType.Left)
            .WhereAll();

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(3);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
        results.Value[2].Id.Should().Be(entity3.Value.Id);
    }

    [Fact]
    public async Task WhenQueryWithSelectFromInnerJoinAndOtherCollectionNotExists_ThenReturnsNoResults()
    {
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1" }), CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .WhereAll()
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AIntValue, je => je.AIntValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryWithSelectFromInnerJoinOnOtherCollection_ThenReturnsAggregatedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1", AIntValue = 7 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName, CommandEntity.FromType(
            new FirstJoiningTestQueryStoreEntity
                { AStringValue = "avalue1", AIntValue = 9 }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue3" }),
            CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .WhereAll()
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AIntValue, je => je.AIntValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(9);
        results.Value[0].GetValueOrDefault<bool>(nameof(TestJoinedDataStoreEntity.ABooleanValue)).Should().Be(false);
    }

    [Fact]
    public async Task
        WhenQueryWithSelectFromInnerJoinOnOtherCollectionAndWhereOnAProjectField_ThenReturnsAggregatedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1", AIntValue = 7 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName, CommandEntity.FromType(
            new FirstJoiningTestQueryStoreEntity
                { AStringValue = "avalue1", AIntValue = 9 }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue3" }),
            CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .Where(e => e.AFirstIntValue, ConditionOperator.EqualTo, 9)
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AFirstIntValue, je => je.AIntValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AFirstIntValue)).Should().Be(9);
        results.Value[0].GetValueOrDefault<bool>(nameof(TestJoinedDataStoreEntity.ABooleanValue)).Should().Be(false);
    }

    [Fact]
    public async Task
        WhenQueryWithSelectFromInnerJoinAndResultContainsDuplicateFieldNames_ThenReturnsAggregatedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(
            new TestDataStoreEntity
                { AStringValue = "avalue1", AIntValue = 7 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(new TestDataStoreEntity
            { AStringValue = "avalue2", AIntValue = 8 }), CancellationToken.None);

        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName, CommandEntity.FromType(
            new FirstJoiningTestQueryStoreEntity
                { AStringValue = "avalue1", AIntValue = 9 }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName, CommandEntity.FromType(
            new FirstJoiningTestQueryStoreEntity
                { AStringValue = "avalue3", AIntValue = 10 }), CancellationToken.None);

        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .WhereAll()
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AIntValue, je => je.AIntValue)
            .Select(e => e.AIntValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(9);
    }

    [Fact]
    public async Task WhenQueryWithSelectFromLeftJoinAndOtherCollectionNotExists_ThenReturnsUnAggregatedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1", AIntValue = 7 }),
            CancellationToken.None);
        var query = Query.From<TestDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue, JoinType.Left)
            .WhereAll()
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AIntValue, je => je.AIntValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(7);
    }

    [Fact]
    public async Task WhenQueryWithSelectFromLeftJoinOnOtherCollection_ThenReturnsPartiallyAggregatedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1", AIntValue = 7 }),
            CancellationToken.None);
        var entity2 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2", AIntValue = 7 }),
            CancellationToken.None);
        var entity3 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue3", AIntValue = 7 }),
            CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName, CommandEntity.FromType(
            new FirstJoiningTestQueryStoreEntity
                { AStringValue = "avalue1", AIntValue = 9 }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue5" }),
            CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue, JoinType.Left)
            .WhereAll()
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AIntValue, je => je.AIntValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);
        results = results.Value.OrderBy(x => x.Id.Value).ToList();

        results.Value.Count.Should().Be(3);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(9);
        results.Value[1].Id.Should().Be(entity2.Value.Id);
        results.Value[1].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(7);
        results.Value[2].Id.Should().Be(entity3.Value.Id);
        results.Value[2].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(7);
    }

    [Fact]
    public async Task WhenQueryWithSelectFromInnerJoinOnMultipleOtherCollections_ThenReturnsAggregatedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(
            new TestDataStoreEntity
                { AStringValue = "avalue1", AIntValue = 7, ALongValue = 7 }), CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName, CommandEntity.FromType(
            new FirstJoiningTestQueryStoreEntity
                { AStringValue = "avalue1", AIntValue = 9 }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue3" }),
            CancellationToken.None);
        await _secondJoiningSetup.Store.AddAsync(_secondJoiningSetup.ContainerName,
            CommandEntity.FromType(new SecondJoiningTestQueryStoreEntity
                { AStringValue = "avalue1", AIntValue = 9, ALongValue = 8 }), CancellationToken.None);
        await _secondJoiningSetup.Store.AddAsync(_secondJoiningSetup.ContainerName,
            CommandEntity.FromType(new SecondJoiningTestQueryStoreEntity { AStringValue = "avalue3" }),
            CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .AndJoin<SecondJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .WhereAll()
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AIntValue, je => je.AIntValue)
            .SelectFromJoin<SecondJoiningTestQueryStoreEntity, long>(e => e.ALongValue, je => je.ALongValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().Be(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(9);
        results.Value[0].GetValueOrDefault<long>(nameof(TestJoinedDataStoreEntity.ALongValue)).Should().Be(8);
    }

    [Fact]
    public async Task WhenQueryWithSelectFromInnerJoinAndOrderByOnAProjectField_ThenReturnsAggregatedResults()
    {
        var entity1 = await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue1", AIntValue = 7 }),
            CancellationToken.None);
        await Setup.Store.AddAsync(Setup.ContainerName,
            CommandEntity.FromType(new TestDataStoreEntity { AStringValue = "avalue2" }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName, CommandEntity.FromType(
            new FirstJoiningTestQueryStoreEntity
                { AStringValue = "avalue1", AIntValue = 9 }), CancellationToken.None);
        await _firstJoiningSetup.Store.AddAsync(_firstJoiningSetup.ContainerName,
            CommandEntity.FromType(new FirstJoiningTestQueryStoreEntity { AStringValue = "avalue3" }),
            CancellationToken.None);
        var query = Query.From<TestJoinedDataStoreEntity>()
            .Join<FirstJoiningTestQueryStoreEntity, string>(e => e.AStringValue, j => j.AStringValue)
            .WhereAll()
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, int>(e => e.AIntValue, je => je.AIntValue)
            .SelectFromJoin<FirstJoiningTestQueryStoreEntity, string>(e => e.AFirstStringValue, je => je.AStringValue)
            .OrderBy(je => je.AFirstStringValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestJoinedDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(1);
        results.Value[0].Id.Should().BeSome(entity1.Value.Id);
        results.Value[0].GetValueOrDefault<int>(nameof(TestJoinedDataStoreEntity.AIntValue)).Should().Be(9);
        results.Value[0].GetValueOrDefault<bool>(nameof(TestJoinedDataStoreEntity.ABooleanValue)).Should().Be(false);
    }

    [Fact]
    public async Task WhenQueryAndNoOrderBy_ThenReturnsResultsSortedByLastPersistedAtUtcAscending()
    {
        var entities = CreateMultipleEntities(ReasonableNumberOfEntitiesToSort);

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll();

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResults(results, entities);
    }

    [Fact]
    public async Task WhenQueryAndOrderByOnUnSelectedField_ThenReturnsResultsSortedById()
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{ReasonableNumberOfEntitiesToSort - counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .Select(e => e.Id)
            .OrderBy(e => e.AStringValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResults(results, entities);
    }

    [Fact]
    public async Task WhenQueryAndOrderByProperty_ThenReturnsResultsSortedByPropertyAscending()
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{ReasonableNumberOfEntitiesToSort - counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .OrderBy(e => e.AStringValue);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResultsInReverse(results, entities);
    }

    [Fact]
    public async Task WhenQueryAndOrderByPropertyDescending_ThenReturnsResultsSortedByPropertyDescending()
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .OrderBy(e => e.AStringValue, OrderDirection.Descending);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResultsInReverse(results, entities);
    }

    [Fact]
    public async Task WhenQueryAndNoTake_ThenReturnsAllResults()
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll();

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResults(results, entities);
    }

    [Fact]
    public async Task WhenQueryAndZeroTake_ThenReturnsNoResults()
    {
        CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
            (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .Take(0);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenQueryAndTakeLessThanAvailable_ThenReturnsAsManyResults()
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .Take(10);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResults(results, entities, 0, 10);
    }

    [Fact]
    public async Task WhenQueryAndTakeMoreThanAvailable_ThenReturnsAllResults()
    {
        var entities =
            CreateMultipleEntities(10, (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .Take(ReasonableNumberOfEntitiesToSort);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResults(results, entities, 0, 10);
    }

    [Fact]
    public async Task WhenQueryAndTakeAndOrderByPropertyDescending_ThenReturnsAsManyResults()
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .OrderBy(e => e.AStringValue, OrderDirection.Descending)
            .Take(10);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResultsInReverse(results, entities, 0, 10);
    }

    [Fact]
#pragma warning disable S4144
    public async Task WhenQueryAndTakeAndOrderByDescending_ThenReturnsFirstPageOfResults()
#pragma warning restore S4144
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .OrderBy(e => e.AStringValue, OrderDirection.Descending)
            .Take(10);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResultsInReverse(results, entities, 0, 10);
    }

    [Fact]
    public async Task WhenQueryAndTakeAndSkipAndOrderBy_ThenReturnsNextPageOfResults()
    {
        var entities =
            CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
                (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .OrderBy(e => e.AStringValue)
            .Skip(10)
            .Take(10);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        VerifyOrderedResultsInReverse(results, entities, 10, 10);
    }

    [Fact]
    public async Task WhenQueryAndTakeAndSkipAllAvailable_ThenReturnsNoResults()
    {
        CreateMultipleEntities(ReasonableNumberOfEntitiesToSort,
            (counter, entity) => entity.AStringValue = $"avalue{counter:000}");

        var query = Query.From<TestDataStoreEntity>()
            .WhereAll()
            .OrderBy(e => e.AStringValue)
            .Skip(ReasonableNumberOfEntitiesToSort)
            .Take(10);

        var results = await Setup.Store.QueryAsync(Setup.ContainerName, query,
            PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        results.Value.Count.Should().Be(0);
    }

    [Fact]
    public async Task WhenRemoveWithNullId_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.RemoveAsync(Setup.ContainerName, null!, CancellationToken.None)).Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenRemoveWithNullContainer_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.RemoveAsync(null!, "anid".ToId(), CancellationToken.None)).Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenRemoveAndEntityExists_ThenDeletesEntity()
    {
        var entity = new CommandEntity("anid");
        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);

        await Setup.Store.RemoveAsync(Setup.ContainerName, entity.Id, CancellationToken.None);

        var count = await Setup.Store.CountAsync(Setup.ContainerName, CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task WhenRemoveAndEntityNotExists_ThenReturns()
    {
        await Setup.Store.RemoveAsync(Setup.ContainerName, "anid".ToId(), CancellationToken.None);

        var count = await Setup.Store.CountAsync(Setup.ContainerName, CancellationToken.None);

        count.Should().Be(0);
    }

    [Fact]
    public async Task WhenReplaceWithNullId_ThenThrows()
    {
        await Setup.Store
            .Invoking(
                x => x.ReplaceAsync(Setup.ContainerName, null!, new CommandEntity("anid"), CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenReplaceWithNullContainer_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.ReplaceAsync(null!, "anid".ToId(), new CommandEntity("anid"), CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenReplaceWithNullEntity_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.ReplaceAsync(Setup.ContainerName, "anid".ToId(), null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenReplaceExisting_ThenReturnsUpdated()
    {
        var entity = new CommandEntity("anid");
        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);

        entity.Add(nameof(TestDataStoreEntity.AStringValue), "updated");
        var updated = await Setup.Store.ReplaceAsync(Setup.ContainerName, entity.Id, entity,
            CancellationToken.None);

        updated.Value.Value.Id.Should().Be(entity.Id);
        updated.Value.Value.Properties.GetValueOrDefault(nameof(TestDataStoreEntity.AStringValue)).Should()
            .Be("updated");
        updated.Value.Value.LastPersistedAtUtc.Should().BeNear(DateTime.UtcNow);
    }

    [Fact]
    public async Task WhenRetrieveWithNullId_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.RetrieveAsync(Setup.ContainerName, null!, PersistedEntityMetadata.Empty,
                CancellationToken.None)).Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenRetrieveWithNullContainer_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.RetrieveAsync(null!, "anid".ToId(), PersistedEntityMetadata.Empty, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenRetrieveWithNullMetadata_ThenThrows()
    {
        await Setup.Store
            .Invoking(x => x.RetrieveAsync(Setup.ContainerName, "anid".ToId(), null!, CancellationToken.None)).Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenRetrieveAndNoData_ThenReturnsNone()
    {
        var entity =
            await Setup.Store.RetrieveAsync(Setup.ContainerName, "anid".ToId(),
                PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        entity.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenRetrieveAndNotExists_ThenReturnsNone()
    {
        var entity1 = new CommandEntity("anid1");
        await Setup.Store.AddAsync(Setup.ContainerName, entity1, CancellationToken.None);

        var entity2 =
            await Setup.Store.RetrieveAsync(Setup.ContainerName, "anid2".ToId(),
                PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        entity2.Value.Should().BeNone();
    }

    [Fact]
    public async Task WhenRetrieveAndExists_ThenReturnsEntity()
    {
        var entity = CommandEntity.FromType(new TestDataStoreEntity
        {
            AStringValue = "astringvalue",
            AnOptionalStringValue = "astringvalue",
            AnOptionalNullableStringValue = "astringvalue",
            EnumValue = TestEnum.AValue1,
            AnOptionalEnumValue = TestEnum.AValue1.ToOptional(),
            AnNullableEnumValue = TestEnum.AValue1,
            ABinaryValue = new byte[] { 0x01 },
            ABooleanValue = true,
            ANullableBooleanValue = true,
            ADoubleValue = 0.1,
            ANullableDoubleValue = 0.1,
            AGuidValue = new Guid("12345678-1111-2222-3333-123456789012"),
            ANullableGuidValue = new Guid("12345678-1111-2222-3333-123456789012"),
            AIntValue = 1,
            ANullableIntValue = 1,
            ALongValue = 2,
            ANullableLongValue = 2,
            ADateTimeUtcValue = DateTime.Today.ToUniversalTime(),
            ANullableDateTimeUtcValue = DateTime.Today.ToUniversalTime(),
            AnOptionalDateTimeUtcValue = DateTime.Today.ToUniversalTime(),
            AnOptionalNullableDateTimeUtcValue = DateTime.Today.ToUniversalTime(),
            ADateTimeOffsetValue = DateTimeOffset.UnixEpoch.ToUniversalTime(),
            ANullableDateTimeOffsetValue = DateTimeOffset.UnixEpoch.ToUniversalTime(),
            AComplexObjectValue =
                new TestComplexObject
                {
                    APropertyValue = "avalue"
                },
            AnOptionalComplexObjectValue =
                new TestComplexObject
                {
                    APropertyValue = "avalue"
                },
            AValueObjectValue = TestValueObject.Create("avalue", 25, true),
            AnOptionalValueObjectValue = TestValueObject.Create("avalue", 25, true)
        });

        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);

        var result =
            await Setup.Store.RetrieveAsync(Setup.ContainerName, entity.Id,
                PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        result.Value.Value.Id.Should().BeSome(entity.Id);
        result.Value.Value.LastPersistedAtUtc.Should().BeNear(DateTime.UtcNow);
        result.Value.Value.LastPersistedAtUtc.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Value.GetValueOrDefault<string>(nameof(TestDataStoreEntity.AStringValue)).Should()
            .Be("astringvalue");
        result.Value.Value.GetValueOrDefault<Optional<string>>(nameof(TestDataStoreEntity.AnOptionalStringValue))
            .Should()
            .BeSome("astringvalue");
        result.Value.Value
            .GetValueOrDefault<Optional<string?>>(nameof(TestDataStoreEntity.AnOptionalNullableStringValue)).Should()
            .BeSome("astringvalue");
        result.Value.Value.GetValueOrDefault<TestEnum>(nameof(TestDataStoreEntity.EnumValue)).Should()
            .Be(TestEnum.AValue1);
        result.Value.Value.GetValueOrDefault<TestEnum?>(nameof(TestDataStoreEntity.AnNullableEnumValue)).Should()
            .Be(TestEnum.AValue1);
        result.Value.Value.GetValueOrDefault<Optional<TestEnum>>(nameof(TestDataStoreEntity.AnOptionalEnumValue))
            .Should()
            .Be(TestEnum.AValue1.ToOptional());
        result.Value.Value.GetValueOrDefault<byte[]>(nameof(TestDataStoreEntity.ABinaryValue))!
            .SequenceEqual(new byte[] { 0x01 }).Should().BeTrue();
        result.Value.Value.GetValueOrDefault<bool>(nameof(TestDataStoreEntity.ABooleanValue)).Should().Be(true);
        result.Value.Value.GetValueOrDefault<bool?>(nameof(TestDataStoreEntity.ANullableBooleanValue)).Should()
            .Be(true);
        result.Value.Value.GetValueOrDefault<Guid>(nameof(TestDataStoreEntity.AGuidValue)).Should()
            .Be(new Guid("12345678-1111-2222-3333-123456789012"));
        result.Value.Value.GetValueOrDefault<Guid?>(nameof(TestDataStoreEntity.ANullableGuidValue)).Should()
            .Be(new Guid("12345678-1111-2222-3333-123456789012"));
        result.Value.Value.GetValueOrDefault<int>(nameof(TestDataStoreEntity.AIntValue)).Should().Be(1);
        result.Value.Value.GetValueOrDefault<int?>(nameof(TestDataStoreEntity.ANullableIntValue)).Should().Be(1);
        result.Value.Value.GetValueOrDefault<long>(nameof(TestDataStoreEntity.ALongValue)).Should().Be(2);
        result.Value.Value.GetValueOrDefault<long?>(nameof(TestDataStoreEntity.ANullableLongValue)).Should().Be(2);
        result.Value.Value.GetValueOrDefault<double>(nameof(TestDataStoreEntity.ADoubleValue)).Should().Be(0.1);
        result.Value.Value.GetValueOrDefault<double?>(nameof(TestDataStoreEntity.ANullableDoubleValue)).Should()
            .Be(0.1);
        result.Value.Value.GetValueOrDefault<DateTime>(nameof(TestDataStoreEntity.ADateTimeUtcValue)).Should()
            .Be(DateTime.Today.ToUniversalTime());
        result.Value.Value.GetValueOrDefault<DateTime>(nameof(TestDataStoreEntity.ADateTimeUtcValue)).Kind
            .Should().Be(DateTimeKind.Utc);
        result.Value.Value.GetValueOrDefault<DateTime?>(nameof(TestDataStoreEntity.ANullableDateTimeUtcValue)).Should()
            .Be(DateTime.Today.ToUniversalTime());
        result.Value.Value.GetValueOrDefault<DateTime?>(nameof(TestDataStoreEntity.ANullableDateTimeUtcValue))!.Value
            .Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Value.GetValueOrDefault<Optional<DateTime>>(nameof(TestDataStoreEntity.AnOptionalDateTimeUtcValue))
            .Value
            .Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Value
            .GetValueOrDefault<Optional<DateTime?>>(nameof(TestDataStoreEntity.AnOptionalNullableDateTimeUtcValue))
            .Value!
            .Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Value.GetValueOrDefault<DateTimeOffset>(nameof(TestDataStoreEntity.ADateTimeOffsetValue)).Should()
            .Be(DateTimeOffset.UnixEpoch.ToUniversalTime());
        result.Value.Value.GetValueOrDefault<DateTimeOffset?>(nameof(TestDataStoreEntity.ANullableDateTimeOffsetValue))
            .Should().Be(DateTimeOffset.UnixEpoch.ToUniversalTime());
        result.Value.Value
            .GetValueOrDefault<TestComplexObject>(nameof(TestDataStoreEntity.AComplexObjectValue)).Should()
            .Be(new TestComplexObject { APropertyValue = "avalue" });
        result.Value.Value
            .GetValueOrDefault<Optional<TestComplexObject>>(nameof(TestDataStoreEntity.AnOptionalComplexObjectValue))
            .Should()
            .BeSome(new TestComplexObject { APropertyValue = "avalue" });
        result.Value.Value
            .GetValueOrDefault<TestValueObject>(nameof(TestDataStoreEntity.AValueObjectValue), DomainFactory)
            .Should().Be(TestValueObject.Create("avalue", 25, true));
        result.Value.Value
            .GetValueOrDefault<Optional<TestValueObject>>(nameof(TestDataStoreEntity.AnOptionalValueObjectValue),
                DomainFactory)
            .Should().BeSome(TestValueObject.Create("avalue", 25, true));
    }

    [Fact]
    public async Task WhenRetrieveAndExistsWithDefaultValues_ThenReturnsEntity()
    {
        var entity = CommandEntity.FromType(new TestDataStoreEntity
        {
            EnumValue = default,
            AnOptionalEnumValue = default,
            AnNullableEnumValue = default,
            ABinaryValue = default!,
            ABooleanValue = default,
            ANullableBooleanValue = default,
            ADoubleValue = default,
            ANullableDoubleValue = default,
            AGuidValue = Guid.Empty,
            ANullableGuidValue = default,
            AIntValue = default,
            ANullableIntValue = default,
            ALongValue = default,
            ANullableLongValue = default,
            AStringValue = default!,
            AnOptionalStringValue = default,
            AnOptionalNullableStringValue = default,
            ADateTimeUtcValue = default,
            ANullableDateTimeUtcValue = default,
            AnOptionalDateTimeUtcValue = default,
            AnOptionalNullableDateTimeUtcValue = default,
            ADateTimeOffsetValue = default,
            ANullableDateTimeOffsetValue = default,
            AComplexObjectValue = default!,
            AnOptionalComplexObjectValue = default,
            AValueObjectValue = default!,
            AnOptionalValueObjectValue = default
        });

        await Setup.Store.AddAsync(Setup.ContainerName, entity, CancellationToken.None);

        var result =
            await Setup.Store.RetrieveAsync(Setup.ContainerName, entity.Id,
                PersistedEntityMetadata.FromType<TestDataStoreEntity>(), CancellationToken.None);

        result.Value.Value.Id.Should().Be(entity.Id);
        result.Value.Value.LastPersistedAtUtc.Should().BeNear(DateTime.UtcNow);
        result.Value.Value.LastPersistedAtUtc.Value.Kind.Should().Be(DateTimeKind.Utc);
        result.Value.Value.GetValueOrDefault<string>(nameof(TestDataStoreEntity.AStringValue)).Should().BeNull();
        result.Value.Value.GetValueOrDefault<Optional<string>>(nameof(TestDataStoreEntity.AnOptionalStringValue))
            .Should().BeNone();
        result.Value.Value
            .GetValueOrDefault<Optional<string?>>(nameof(TestDataStoreEntity.AnOptionalNullableStringValue)).Should()
            .BeNone();
        result.Value.Value.GetValueOrDefault<TestEnum>(nameof(TestDataStoreEntity.EnumValue)).Should()
            .Be(TestEnum.NoValue);
        result.Value.Value.GetValueOrDefault<TestEnum?>(nameof(TestDataStoreEntity.AnNullableEnumValue)).Should()
            .BeNull();
        result.Value.Value.GetValueOrDefault<Optional<TestEnum>>(nameof(TestDataStoreEntity.AnOptionalEnumValue))
            .ValueOrDefault.Should()
            .Be(Optional<TestEnum>.None);
        result.Value.Value.GetValueOrDefault<byte[]>(nameof(TestDataStoreEntity.ABinaryValue)).Should().BeNull();
        result.Value.Value.GetValueOrDefault<bool>(nameof(TestDataStoreEntity.ABooleanValue)).Should().Be(false);
        result.Value.Value.GetValueOrDefault<bool?>(nameof(TestDataStoreEntity.ANullableBooleanValue)).Should()
            .BeNull();
        result.Value.Value.GetValueOrDefault<Guid>(nameof(TestDataStoreEntity.AGuidValue)).Should().Be(Guid.Empty);
        result.Value.Value.GetValueOrDefault<Guid?>(nameof(TestDataStoreEntity.ANullableGuidValue)).Should().BeNull();
        result.Value.Value.GetValueOrDefault<int>(nameof(TestDataStoreEntity.AIntValue)).Should().Be(0);
        result.Value.Value.GetValueOrDefault<int?>(nameof(TestDataStoreEntity.ANullableIntValue)).Should().BeNull();
        result.Value.Value.GetValueOrDefault<long>(nameof(TestDataStoreEntity.ALongValue)).Should().Be(0L);
        result.Value.Value.GetValueOrDefault<long?>(nameof(TestDataStoreEntity.ANullableLongValue)).Should().BeNull();
        result.Value.Value.GetValueOrDefault<double>(nameof(TestDataStoreEntity.ADoubleValue)).Should().Be(0.0D);
        result.Value.Value.GetValueOrDefault<double?>(nameof(TestDataStoreEntity.ANullableDoubleValue)).Should()
            .BeNull();
        result.Value.Value.GetValueOrDefault<DateTime>(nameof(TestDataStoreEntity.ADateTimeUtcValue)).Should()
            .Be(DateTime.MinValue);
        result.Value.Value.GetValueOrDefault<DateTime>(nameof(TestDataStoreEntity.ADateTimeUtcValue)).Kind.Should()
            .Be(DateTimeKind.Utc);
        result.Value.Value.GetValueOrDefault<DateTime?>(nameof(TestDataStoreEntity.ANullableDateTimeUtcValue)).Should()
            .BeNull();
        result.Value.Value.GetValueOrDefault<Optional<DateTime>>(nameof(TestDataStoreEntity.AnOptionalDateTimeUtcValue))
            .Should()
            .BeSome(DateTime.MinValue);
        result.Value.Value
            .GetValueOrDefault<Optional<DateTime?>>(nameof(TestDataStoreEntity.AnOptionalNullableDateTimeUtcValue))
            .Should()
            .BeNone();
        result.Value.Value.GetValueOrDefault<DateTimeOffset>(nameof(TestDataStoreEntity.ADateTimeOffsetValue)).Should()
            .Be(DateTimeOffset.MinValue);
        result.Value.Value.GetValueOrDefault<DateTimeOffset>(nameof(TestDataStoreEntity.ADateTimeOffsetValue)).Date.Kind
            .Should().Be(DateTimeKind.Unspecified);
        result.Value.Value.GetValueOrDefault<DateTimeOffset?>(nameof(TestDataStoreEntity.ANullableDateTimeOffsetValue))
            .Should().BeNull();
        result.Value.Value.GetValueOrDefault<TestComplexObject>(nameof(TestDataStoreEntity.AComplexObjectValue))
            .Should().BeNull();
        result.Value.Value
            .GetValueOrDefault<Optional<TestComplexObject>>(nameof(TestDataStoreEntity.AnOptionalComplexObjectValue))
            .Should()
            .BeNone();
        result.Value.Value
            .GetValueOrDefault<TestValueObject>(nameof(TestDataStoreEntity.AValueObjectValue), DomainFactory)
            .Should().BeNull();
        result.Value.Value
            .GetValueOrDefault<Optional<TestValueObject>>(nameof(TestDataStoreEntity.AnOptionalValueObjectValue),
                DomainFactory)
            .Should().BeNone();
    }

    public struct DataStoreInfo
    {
        public required IDataStore Store { get; set; }

        public required string ContainerName { get; set; }
    }

    protected IDataStore DataStore => Setup.Store;

    protected string ContainerName => Setup.ContainerName;

    private List<Optional<string>> CreateMultipleEntities(int count,
        Action<int, TestDataStoreEntity>? factory = null)
    {
        var createdIdentifiers = new List<Optional<string>>();
        Repeat.Times(counter =>
        {
            var entity = new TestDataStoreEntity();
            factory?.Invoke(counter, entity);
            Setup.Store.AddAsync(Setup.ContainerName, CommandEntity.FromType(entity),
                CancellationToken.None).GetAwaiter().GetResult();
            createdIdentifiers.Add(entity.Id);
            WaitSomeTimeToIntroduceTimeDelayForSortingDates();
        }, count);

        return createdIdentifiers;

        //HACK: We use this method so that dates of the created records are more than a few milliseconds apart,
        //since calls to DateTime.UtcNow have resolution issues less than ~15ms.
        // This makes tests run at least very slow!
        static void WaitSomeTimeToIntroduceTimeDelayForSortingDates()
        {
            Thread.Sleep(ReasonableTimeDelayBetweenTimestamps);
        }
    }

    private static void VerifyOrderedResultsInReverse(Result<List<QueryEntity>, Error> results,
        List<Optional<string>> entities,
        int? offset = null, int? limit = null)
    {
        entities.Reverse();
        VerifyOrderedResults(results, entities, offset, limit);
    }

    private static void VerifyOrderedResults(Result<List<QueryEntity>, Error> results,
        IReadOnlyList<Optional<string>> entities,
        int? offset = null, int? limit = null)
    {
        var expectedResultCount = limit ?? entities.Count;
        results.Value.Count.Should().Be(expectedResultCount);

        var resultIndex = 0;
        var entityCount = 0;
        results.Value.ForEach(result =>
        {
            if (limit.HasValue && entityCount >= limit.Value)
            {
                return;
            }

            if (!offset.HasValue || resultIndex >= offset)
            {
                var createdIdentifier = entities[resultIndex];

                result.Id.Should().Be(createdIdentifier,
                    $"Result at ({resultIndex}) should have been: {createdIdentifier}");

                entityCount++;
            }

            resultIndex++;
        });
    }
}