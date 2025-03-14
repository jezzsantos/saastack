using RabbitMQ.Client;

namespace OnPremisesWorkerService.Core.Abstractions;

public interface IRabbitMqListenerServiceConnection : IDisposable
{
    bool IsConnected { get; }
    IModel CreateModel();
    void TryConnect();
}