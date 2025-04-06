using RabbitMQ.Client;

namespace OnPremRelay.WorkerHost.Messaging.Interfaces;

public interface IRabbitMqConnection : IDisposable
{
    IModel CreateModel();
}