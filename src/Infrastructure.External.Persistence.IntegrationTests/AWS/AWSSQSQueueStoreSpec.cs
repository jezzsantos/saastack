using Common;
using FluentAssertions;
using Infrastructure.External.Persistence.AWS;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.External.Persistence.IntegrationTests.AWS;

[Trait("Category", "Integration.Persistence")]
[Collection("AWSAccount")]
public class AWSSQSQueueStoreSpec : AnyQueueStoreBaseSpec
{
    private readonly AWSAccountSpecSetup _setup;

    public AWSSQSQueueStoreSpec(AWSAccountSpecSetup setup) : base(setup.QueueStore)
    {
        _setup = setup;
    }

    [Fact]
    public async Task WhenPushWithInvalidQueueName_ThenThrows()
    {
        await _setup.QueueStore
            .Invoking(x => x.PushAsync("^aninvalidqueuename^", "amessage", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidQueueName);
    }

    [Fact]
    public async Task WhenPopSingleWithInvalidQueueName_ThenThrows()
    {
        await _setup.QueueStore
            .Invoking(x =>
                x.PopSingleAsync("^aninvalidqueuename^", (_, _) => Task.FromResult(Result.Ok), CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidQueueName);
    }

    [Fact]
    public async Task WhenCountWithInvalidQueueName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.QueueStore
            .Invoking(x => x.CountAsync("^aninvalidqueuename^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidQueueName);
#endif
    }

    [Fact]
    public async Task WhenDestroyAllWithInvalidQueueName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.QueueStore
            .Invoking(x => x.DestroyAllAsync("^aninvalidqueuename^", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.ValidationExtensions_InvalidQueueName);
#endif
    }
}