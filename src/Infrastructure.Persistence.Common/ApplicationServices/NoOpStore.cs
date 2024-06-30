using System.Diagnostics.CodeAnalysis;
using Application.Persistence.Interfaces;
using Common;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using QueryAny;
#if TESTINGONLY
using Infrastructure.Persistence.Interfaces.ApplicationServices;
#endif

namespace Infrastructure.Persistence.Common.ApplicationServices;

/// <summary>
///     Defines a combined store that does nothing
/// </summary>
[ExcludeFromCodeCoverage]
[UsedImplicitly]
public class NoOpStore : IDataStore, IBlobStore, IQueueStore, IMessageBusStore, IEventStore
{
    private NoOpStore()
    {
    }

    public static NoOpStore Instance => new();

    public Task<Result<Error>> DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }

#if TESTINGONLY
    Task<Result<Error>> IBlobStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
#endif

    public Task<Result<Optional<Blob>, Error>> DownloadAsync(string containerName, string blobName, Stream stream,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<Optional<Blob>, Error>>(Optional<Blob>.None);
    }

    public Task<Result<Error>> UploadAsync(string containerName, string blobName, string contentType, Stream stream,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<CommandEntity, Error>> AddAsync(string containerName, CommandEntity entity,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<CommandEntity, Error>>(default);
    }

    Task<Result<long, Error>> IDataStore.CountAsync(string containerName, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<long, Error>>(0);
    }

#if TESTINGONLY
    Task<Result<Error>> IDataStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
#endif

    public int MaxQueryResults => 0;

    public Task<Result<List<QueryEntity>, Error>> QueryAsync<TQueryableEntity>(string containerName,
        QueryClause<TQueryableEntity> query, PersistedEntityMetadata metadata,
        CancellationToken cancellationToken)
        where TQueryableEntity : IQueryableEntity
    {
        return Task.FromResult(Result<List<QueryEntity>, Error>.FromResult(new List<QueryEntity>()));
    }

    public Task<Result<Error>> RemoveAsync(string containerName, string id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Optional<CommandEntity>, Error>> ReplaceAsync(string containerName, string id,
        CommandEntity entity, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<Optional<CommandEntity>, Error>.FromResult(Optional<CommandEntity>.None));
    }

    public Task<Result<Optional<CommandEntity>, Error>> RetrieveAsync(string containerName, string id,
        PersistedEntityMetadata metadata,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<Optional<CommandEntity>, Error>.FromResult(Optional<CommandEntity>.None));
    }

    public Task<Result<string, Error>> AddEventsAsync(string entityName, string entityId,
        List<EventSourcedChangeEvent> events, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string, Error>.FromResult(string.Empty));
    }

#if TESTINGONLY
    Task<Result<Error>> IEventStore.DestroyAllAsync(string entityName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
#endif

    public Task<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>> GetEventStreamAsync(string entityName,
        string entityId, CancellationToken cancellationToken)
    {
        return Task.FromResult(
            Result<IReadOnlyList<EventSourcedChangeEvent>, Error>.FromResult(new List<EventSourcedChangeEvent>()));
    }

#if TESTINGONLY
    Task<Result<long, Error>> IMessageBusStore.CountAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<long, Error>.FromResult(0));
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IMessageBusStore.DestroyAllAsync(string topicName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
#endif

#if TESTINGONLY
    Task<Result<bool, Error>> IMessageBusStore.ReceiveSingleAsync(string topicName, string subscriptionName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<bool, Error>.FromResult(false));
    }
#endif

    Task<Result<Error>> IMessageBusStore.SendAsync(string topicName, string message,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> SubscribeAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }

#if TESTINGONLY
    Task<Result<long, Error>> IQueueStore.CountAsync(string queueName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<long, Error>.FromResult(0));
    }
#endif

#if TESTINGONLY
    Task<Result<Error>> IQueueStore.DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }
#endif

    Task<Result<bool, Error>> IQueueStore.PopSingleAsync(string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<bool, Error>.FromResult(false));
    }

    Task<Result<Error>> IQueueStore.PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Ok);
    }

#if TESTINGONLY
#pragma warning disable CS0067 // Event is never used
    public event MessageQueueUpdated? OnMessagesQueued;
#pragma warning restore CS0067 // Event is never used
#endif
}