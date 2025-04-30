namespace OnPremRelay.WorkerHost.Configuration;

public class RabbitMqSettings
{
    public string HostName { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public int Port { get; set; }

    public string VirtualHost { get; set; }
}