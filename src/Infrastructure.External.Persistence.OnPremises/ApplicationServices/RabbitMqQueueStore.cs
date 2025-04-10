using System.Text;
using Common;
using Common.Extensions;
using Infrastructure.External.Persistence.OnPremises.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace Infrastructure.External.Persistence.OnPremises.ApplicationServices;

[UsedImplicitly]
public class RabbitMqQueueStore : IQueueStore, IAsyncDisposable
{
    private readonly RabbitMqStoreOptions _options;
    private readonly Dictionary<string, bool> _queueExistenceChecks = new();
    private readonly IRecorder _recorder;
    private static readonly Encoding Utf8Encoding = Encoding.UTF8;
    private const int DefaultRabbitMqPort = 5672;
    private IConnection? _connection;
    private readonly object _connectionLock = new();

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
        queueName.ThrowIfNotValuedParameter(nameof(queueName),
            Resources.AnyStore_MissingQueueName);

        var channel = await ConnectToQueueAsync(queueName, cancellationToken);
        try
        {
            channel.QueueDelete(queueName, false, false);
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

        BasicGetResult? messageResult;
        try
        {
            messageResult = channel.BasicGet(queueName, false);
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex,
                "Failed to retrieve message from queue: {Queue}", queueName);
            channel.Dispose();
            return ex.ToError(ErrorCode.Unexpected);
        }

        if (messageResult.NotExists())
        {
            _recorder.TraceInformation(null, "No message on queue: {Queue}", queueName);
            channel.Dispose();
            return false;
        }

        var body = Utf8Encoding.GetString(messageResult.Body.ToArray());

        try
        {
            var handled = await messageHandlerAsync(body, cancellationToken);
            if (handled.IsFailure)
            {
                channel.BasicNack(messageResult.DeliveryTag, false, true);
                channel.Dispose();
                return handled.Error;
            }
        }
        catch (Exception ex)
        {
            channel.BasicNack(messageResult.DeliveryTag, false, true);

            _recorder.TraceError(null, ex,
                "Failed to handle last message: {MessageId} from queue: {Queue}",
                messageResult.DeliveryTag, queueName);
            channel.Dispose();
            return ex.ToError(ErrorCode.Unexpected);
        }

        try
        {
            channel.BasicAck(messageResult.DeliveryTag, false);
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
            channel.BasicPublish("",
                queueName,
                null,
                body);

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
        cancellationToken.ThrowIfCancellationRequested();

        await Task.CompletedTask;
        var sanitizedQueueName = queueName.SanitizeAndValidateInvalidDatabaseResourceName();

        EnsureConnection();
        cancellationToken.ThrowIfCancellationRequested();

        var channel = _connection!.CreateModel();

        if (IsQueueExistenceCheckPerformed(sanitizedQueueName))
        {
            return channel;
        }

        try
        {
            channel.QueueDeclare(
                sanitizedQueueName,
                true,
                false,
                false,
                null);

            cancellationToken.ThrowIfCancellationRequested();
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
        lock (_connectionLock)
        {
            if (_connection.Exists() && _connection.IsOpen)
            {
                return;
            }

            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port ?? DefaultRabbitMqPort,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = "RabbitMqQueueStore",
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };

            _connection = factory.CreateConnection();
        }
    }
}