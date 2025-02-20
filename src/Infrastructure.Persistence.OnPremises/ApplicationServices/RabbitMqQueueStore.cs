using System.Text;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.OnPremises.Extensions;
using JetBrains.Annotations;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Infrastructure.Persistence.OnPremises.ApplicationServices;

[UsedImplicitly]
public class RabbitMqQueueStore : IQueueStore, IAsyncDisposable
{
    private readonly RabbitMqStoreOptions _options;
    private readonly Dictionary<string, bool> _queueExistenceChecks = new();
    private readonly IRecorder _recorder;

    private IConnection? _connection;

    public static RabbitMqQueueStore Create(IRecorder recorder, RabbitMqStoreOptions options)
    {
        return new RabbitMqQueueStore(recorder, options);
    }

    private RabbitMqQueueStore(IRecorder recorder, RabbitMqStoreOptions options)
    {
        _recorder = recorder;
        _options = options;
    }

    public ValueTask DisposeAsync()
    {
        _connection?.Dispose();
        return ValueTask.CompletedTask;
    }

#if TESTINGONLY
    public async Task<Result<long, Error>> CountAsync(string queueName, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.AnyStore_MissingQueueName);

        var channel = await ConnectToQueueAsync(queueName, cancellationToken);

        try
        {
            var declareOk = channel.QueueDeclarePassive(queueName);
            return declareOk.MessageCount;
        }
        catch (OperationInterruptedException ex)
        {
            _recorder.TraceError(null, ex,
                "Failed to retrieve message count for queue: {Queue}. Reason: {ErrorMessage}",
                queueName, ex.Message);
            return 0;
        }
        finally
        {
            channel.Dispose();
        }
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(string queueName, CancellationToken cancellationToken)
    {
#if TESTINGONLY
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AnyStore_MissingQueueName);

        var channel = await ConnectToQueueAsync(queueName, cancellationToken);
        try
        {
            channel.QueueDelete(queue: queueName, ifUnused: false, ifEmpty: false);
            _queueExistenceChecks.Remove(queueName);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "Failed to delete queue: {Queue}", queueName);
            return ex.ToError(ErrorCode.Unexpected);
        }
        finally
        {
            channel.Dispose();
        }
#else
        await Task.CompletedTask;
#endif
        return Result.Ok;
    }
#endif

    public async Task<Result<bool, Error>> PopSingleAsync(
        string queueName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.AnyStore_MissingQueueName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        var channel = await ConnectToQueueAsync(queueName, cancellationToken);

        BasicGetResult? messageResult = null;
        try
        {
            messageResult = channel.BasicGet(queueName, autoAck: false);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex,
                "Failed to retrieve message from queue: {Queue}", queueName);
            channel.Dispose();
            return ex.ToError(ErrorCode.Unexpected);
        }

        if (messageResult == null)
        {
            _recorder.TraceInformation(null, "No message on queue: {Queue}", queueName);
            channel.Dispose();
            return false;
        }

        var body = Encoding.UTF8.GetString(messageResult.Body.ToArray());

        try
        {
            var handled = await messageHandlerAsync(body, cancellationToken);
            if (handled.IsFailure)
            {
                channel.BasicNack(messageResult.DeliveryTag, multiple: false, requeue: true);
                channel.Dispose();
                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            channel.BasicNack(messageResult.DeliveryTag, multiple: false, requeue: true);

            _recorder.TraceError(null, ex,
                "Failed to handle last message: {MessageId} from queue: {Queue}",
                messageResult.DeliveryTag, queueName);
            channel.Dispose();
            return ex.ToError(ErrorCode.Unexpected);
        }

        try
        {
            channel.BasicAck(messageResult.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex,
                "Failed to acknowledge message: {DeliveryTag} from queue: {Queue}",
                messageResult.DeliveryTag, queueName);
        }

        channel.Dispose();
        return true;
    }

    public async Task<Result<Error>> PushAsync(string queueName, string message, CancellationToken cancellationToken)
    {
        queueName.ThrowIfNotValuedParameter(nameof(queueName), Resources.AnyStore_MissingQueueName);
        message.ThrowIfNotValuedParameter(nameof(message), Resources.AnyStore_MissingMessage);

        var channel = await ConnectToQueueAsync(queueName, cancellationToken);

        try
        {
            var body = Encoding.UTF8.GetBytes(message);
            channel.BasicPublish(exchange: "",
                                 routingKey: queueName,
                                 basicProperties: null,
                                 body: body);

            _recorder.TraceInformation(null, "Added message to queue: {Queue}", queueName);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex,
                "Failed to push message: {Message} to queue: {Queue}",
                message, queueName);
            channel.Dispose();
            return ex.ToError(ErrorCode.Unexpected);
        }

        channel.Dispose();
        return Result.Ok;
    }

    private async Task<IModel> ConnectToQueueAsync(string queueName, CancellationToken cancellationToken)
    {
        var sanitizedQueueName = queueName.SanitizeAndValidateStorageAccountResourceName();

        EnsureConnection();

        var channel = _connection!.CreateModel();

        if (IsQueueExistenceCheckPerformed(sanitizedQueueName))
        {
            return channel;
        }

        try
        {
            channel.QueueDeclare(
                queue: sanitizedQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "Failed to declare queue: {Queue}", sanitizedQueueName);
            throw;
        }

        return channel;
    }

    private bool IsQueueExistenceCheckPerformed(string queueName)
    {
        _queueExistenceChecks.TryAdd(queueName, false);
        if (_queueExistenceChecks[queueName])
        {
            return true;
        }

        _queueExistenceChecks[queueName] = true;
        return false;
    }

    private void EnsureConnection()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port ?? 5672,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost
            };
            _connection = factory.CreateConnection();
        }
    }
}
