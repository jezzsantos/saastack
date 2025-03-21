using Common.Configuration;

namespace Infrastructure.Persistence.OnPremises.ApplicationServices;

public class RabbitMqStoreOptions
{
    private const string HostNameSettingName = "ApplicationServices:Persistence:RabbitMq:HostName";
    private const string UserNameSettingName = "ApplicationServices:Persistence:RabbitMq:UserName";
    private const string PasswordSettingName = "ApplicationServices:Persistence:RabbitMq:Password";
    private const string VirtualHostSettingName = "ApplicationServices:Persistence:RabbitMq:VirtualHost";

    private RabbitMqStoreOptions(
        string hostName,
        string? userName = null,
        string? password = null,
        string? virtualHost = null,
        int? port = null,
        bool useAsyncDispatcher = true)
    {
        HostName = hostName;
        UserName = userName;
        Password = password;
        VirtualHost = virtualHost;
        Port = port;
        UseAsyncDispatcher = useAsyncDispatcher;
    }

    public string HostName { get; }

    public string? UserName { get; }

    public string? Password { get; }

    public string? VirtualHost { get; }

    public int? Port { get; }

    public bool UseAsyncDispatcher { get; }

    public static RabbitMqStoreOptions FromCredentials(IConfigurationSettings settings)
    {
        return new RabbitMqStoreOptions(
            hostName: settings.GetString(HostNameSettingName),
            userName: settings.GetString(UserNameSettingName),
            password: settings.GetString(PasswordSettingName),
            virtualHost: settings.GetString(VirtualHostSettingName));
    }

    public static RabbitMqStoreOptions FromConnectionString(string connectionString)
    {
        var uri = new Uri(connectionString);

        var userInfo = uri.UserInfo.Split(':');
        var userName = userInfo[0];
        var password = userInfo.Length > 1
            ? userInfo[1]
            : string.Empty;

        var hostName = uri.Host;
        var port = uri.Port;

        var virtualHost = uri.AbsolutePath == "/"
            ? "/"
            : uri.AbsolutePath.TrimStart('/');

        return new RabbitMqStoreOptions(
            hostName: hostName,
            userName: userName,
            password: password,
            port: port,
            virtualHost: virtualHost
        );
    }

    private string ToConnectionString()
    {
        var portPart = Port.HasValue
            ? $":{Port}"
            : string.Empty;
        var virtualHostPart = string.IsNullOrEmpty(VirtualHost)
            ? string.Empty
            : $"/{VirtualHost}";
        return $"amqp://{UserName}:{Password}@{HostName}{portPart}{virtualHostPart}";
    }

    public static string BuildConnectionString(
        string hostName,
        string userName,
        string password,
        string virtualHost,
        int port,
        bool useAsyncDispatcher = true)
    {
        var options = new RabbitMqStoreOptions(
            hostName,
            userName,
            password,
            virtualHost,
            port,
            useAsyncDispatcher);
        return options.ToConnectionString();
    }

    public static string BuildConnectionString(RabbitMqStoreOptions options)
    {
        return options.ToConnectionString();
    }

    public static string BuildConnectionString(IConfigurationSettings configuration)
    {
        var options = FromCredentials(configuration);
        return options.ToConnectionString();
    }

    public static string BuildConnectionString(string connectionString)
    {
        var options = FromConnectionString(connectionString);
        return options.ToConnectionString();
    }
}