#if TESTINGONLY
using System.Text.Json;
using Application.Interfaces.Services;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Interfaces.Clients;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Hosting.Common.ApplicationServices;

/// <summary>
///     Provides a background service to regularly and continuously call the Ancillary API that drains messages accumulated
///     on various
///     message queues.
///     Used only in TESTINGONLY and on local machine for specific <see cref="IDataStore" /> implementations, to simulate
///     real triggered message queues running in the cloud.
/// </summary>
public class StubQueueDrainingService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StartInterval = TimeSpan.FromSeconds(5);
    private readonly IHttpJsonClient _apiClient;
    private readonly ILogger _logger;
    private readonly IMonitoredMessageQueues _monitoredMessageQueues;
    private readonly Dictionary<string, IWebRequest> _monitorQueueMappings;
    private readonly string _hmacSecret;

    public StubQueueDrainingService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions,
        IHostSettings settings, ILogger<StubQueueDrainingService> logger,
        IMonitoredMessageQueues monitoredMessageQueues,
        Dictionary<string, IWebRequest> monitoredQueueApiMappings) : this(httpClientFactory, jsonOptions, logger,
        monitoredMessageQueues, monitoredQueueApiMappings, settings.GetAncillaryApiHostBaseUrl(),
        settings.GetAncillaryApiHostHmacAuthSecret())
    {
    }

    private StubQueueDrainingService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions,
        ILogger<StubQueueDrainingService> logger,
        IMonitoredMessageQueues monitoredMessageQueues, Dictionary<string, IWebRequest> monitorQueueMappings,
        string baseUrl, string hmacSecret)
    {
        baseUrl.ThrowIfNotValuedParameter(nameof(baseUrl));
        _logger = logger;
        _monitoredMessageQueues = monitoredMessageQueues;
        _monitorQueueMappings = monitorQueueMappings;
        _hmacSecret = hmacSecret;
        _apiClient = CreateApiClient(httpClientFactory, jsonOptions, baseUrl);
        
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_apiClient is IDisposable disposableApiClient)
        {
            disposableApiClient.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public IEnumerable<string> MonitoredQueues => _monitorQueueMappings.Select(mqm => mqm.Key);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(StartInterval, cancellationToken);
        await DrainQueuesAsync(cancellationToken);
    }

    private static IHttpJsonClient CreateApiClient(IHttpClientFactory httpClientFactory,
        JsonSerializerOptions jsonOptions, string baseUrl)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(baseUrl);
        return new JsonClient(httpClient, jsonOptions);
    }

    private async Task DrainQueuesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var queueName = _monitoredMessageQueues.NextQueueName();
            if (queueName.HasValue)
            {
                try
                {
                    if (_monitorQueueMappings.TryGetValue(queueName, out var webRequest))
                    {
                        await _apiClient.PostAsync(webRequest, req => req.SetHMACAuth(webRequest, _hmacSecret),
                            cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to drain messages for queue {Queue}", queueName);
                }
            }

            await Task.Delay(CheckInterval, cancellationToken);
        }
    }
}
#endif