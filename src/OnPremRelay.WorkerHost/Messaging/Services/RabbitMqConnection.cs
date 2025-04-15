using Microsoft.Extensions.Options;
using OnPremRelay.WorkerHost.Configuration;
using OnPremRelay.WorkerHost.Messaging.Interfaces;
using RabbitMQ.Client;

namespace OnPremRelay.WorkerHost.Messaging.Services;

public class RabbitMqConnection : IRabbitMqConnection
{
    private readonly IConnection _connection;

    public RabbitMqConnection(IOptions<RabbitMqSettings> options)
    {
        var factory = new ConnectionFactory
        {
            HostName = options.Value.HostName,
            UserName = options.Value.UserName,
            Password = options.Value.Password,
            Port = options.Value.Port,
            VirtualHost = options.Value.VirtualHost,
            DispatchConsumersAsync = true,

            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
        };
        _connection = factory.CreateConnection();
    }

    public IModel CreateModel()
    {
        return _connection.CreateModel();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}