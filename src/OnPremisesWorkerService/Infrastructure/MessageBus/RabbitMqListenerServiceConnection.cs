using Microsoft.Extensions.Options;
using OnPremisesWorkerService.Configuration;
using OnPremisesWorkerService.Core.Abstractions;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;

namespace OnPremisesWorkerService.Infrastructure.MessageBus;


public sealed class RabbitMqListenerServiceConnection : IRabbitMqListenerServiceConnection
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqListenerServiceConnection> _logger;
    private IConnection? _connection;
    private bool _disposed;

    public bool IsConnected => _connection?.IsOpen == true && !_disposed;

    public RabbitMqListenerServiceConnection(
        IOptions<RabbitMqSettings> options,
        ILogger<RabbitMqListenerServiceConnection> logger)
    {
        _logger = logger;
        var settings = options.Value;

        _connectionFactory = new ConnectionFactory
        {
            HostName = settings.HostName,
            UserName = settings.UserName,
            Password = settings.Password,
            ClientProvidedName = "OnPremisesWorkerService",
            DispatchConsumersAsync = true
        };

        TryConnect();
    }

    public IModel CreateModel() => _connection?.CreateModel() ?? throw new InvalidOperationException("No RabbitMq connection");

    public void TryConnect()
    {
        try
        {
            if (IsConnected) return;

            _connection = _connectionFactory.CreateConnection();
            _connection.ConnectionShutdown += OnConnectionShutdown;
            _connection.CallbackException += OnCallbackException;
            _connection.ConnectionBlocked += OnConnectionBlocked;

            _logger.LogInformation("RabbitMq connection established");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "RabbitMq connection failed");
            Task.Delay(5000).Wait();
        }
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMq connection blocked. Reason: {Reason}", e.Reason);
        TryConnect();
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMq connection callback exception");
        TryConnect();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMq connection shutdown. Cause: {Cause}", e.Cause);
        TryConnect();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _connection?.Dispose();
    }
}