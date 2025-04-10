using Common;
using FluentAssertions;
using Infrastructure.External.Persistence.OnPremises;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.External.Persistence.IntegrationTests.OnPremises;

[Trait("Category", "Integration.Persistence")]
[Collection("RabbitMqQueue")]
public class RabbitMqQueueStoreSpec : AnyQueueStoreBaseSpec
{
    private readonly RabbitMqQueueSpecSetup _setup;

    public RabbitMqQueueStoreSpec(RabbitMqQueueSpecSetup setup)
        : base(setup.QueueStore)
    {
        _setup = setup;
    }

    [Fact]
    public async Task WhenPushWithInvalidQueueName_ThenThrows()
    {
        await _setup.QueueStore
            .Invoking(x => x.PushAsync("^aninvalidqueuename^", "amessage", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidDatabaseResourceName);
    }

    [Fact]
    public async Task WhenPopSingleWithInvalidQueueName_ThenThrows()
    {
        await _setup.QueueStore
            .Invoking(x => x.PopSingleAsync("^aninvalidqueuename^",
                (_, _) => Task.FromResult(Result.Ok), CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidDatabaseResourceName);
    }

    [Fact]
    public async Task WhenCountWithInvalidQueueName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.QueueStore
            .Invoking(x => x.CountAsync("^aninvalidqueuename^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidDatabaseResourceName);
#endif
    }

    [Fact]
    public async Task WhenDestroyAllWithInvalidQueueName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.QueueStore
            .Invoking(x => x.DestroyAllAsync("^aninvalidqueuename^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidDatabaseResourceName);
#endif
    }
}