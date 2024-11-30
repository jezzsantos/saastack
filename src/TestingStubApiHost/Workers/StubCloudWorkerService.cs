#if TESTINGONLY
using System.Collections.Concurrent;
using System.Text.Json;
using Application.Interfaces;
using Application.Interfaces.Services;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Interfaces.ApplicationServices;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Clients;
using Task = System.Threading.Tasks.Task;

namespace TestingStubApiHost.Workers;

/// <summary>
///     Provides an asynchronous background service that regularly and continuously polls all the
///     queues and message bus topics of <see cref="TestingStubApiHost.QueuedMappings" /> and when found relays them to
///     the Ancillary API and EventNotifications API to drains any messages accumulated.
///     Essentially, this stub replaces stands in for the Azure Functions/AWS Lambdas that are triggered, and call the
///     respective APIs directly, except that these API calls, do the draining themselves.
///     Note: Used only in TESTINGONLY, and only by specific <see cref="IDataStore" /> implementations,
///     that implement <see cref="IQueueStoreTrigger" /> and <see cref="IMessageBusStoreTrigger" />.
///     Note: We need to inject the singleton of the <see cref="LocalMachineJsonFileStore" />
///     so that we are monitoring the local disk for changes in other processes
/// </summary>
public class StubCloudWorkerService : BackgroundService
{
    private static readonly ConcurrentDictionary<string, JsonClient> CachedClients = new();
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StartInterval = TimeSpan.FromSeconds(5);
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger _logger;
    private readonly IMessageMonitor _monitor;
    private readonly Dictionary<string, IWebRequest> _monitorMessageBusTopicMappings;
    private readonly Dictionary<string, IWebRequest> _monitorQueueMappings;
    private readonly IHostSettings _settings;
    private readonly LocalMachineJsonFileStore _store;

    public StubCloudWorkerService(IHostSettings settings, IMessageMonitor monitor, LocalMachineJsonFileStore store,
        IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions, ILogger<StubCloudWorkerService> logger,
        Dictionary<string, IWebRequest> monitorQueueMappings,
        Dictionary<string, IWebRequest> monitorMessageBusTopicMappings)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = jsonOptions;
        _logger = logger;
        _monitorQueueMappings = monitorQueueMappings;
        _monitorMessageBusTopicMappings = monitorMessageBusTopicMappings;
        _settings = settings;
        _monitor = monitor;
        _store = store;
    }

    public override void Dispose()
    {
        base.Dispose();
        if (!CachedClients.IsEmpty)
        {
            foreach (var cachedClient in CachedClients.Values)
            {
                cachedClient.Dispose();
            }
        }

        _store.Dispose();

        GC.SuppressFinalize(this);
    }

    public IEnumerable<string> MonitoredBusTopics => _monitorMessageBusTopicMappings.Select(mbm => mbm.Key);

    public IEnumerable<string> MonitoredQueues => _monitorQueueMappings.Select(mqm => mqm.Key);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(StartInterval, cancellationToken);
        _logger.LogInformation(
            $"Starting the '{nameof(StubCloudWorkerService)}', and polling all queues and message bus topics");

        await Task.WhenAll(
            DrainQueuesAsync(cancellationToken),
            DrainMessageBusTopicsAsync(cancellationToken));

        _logger.LogInformation(
            $"Ending the '{nameof(StubCloudWorkerService)}'");
    }

    private static JsonClient CreateApiEndpointClient(string clientType, IHttpClientFactory httpClientFactory,
        JsonSerializerOptions jsonOptions, string baseUrl)
    {
        var cacheKey = $"{clientType}|{baseUrl}";
        if (CachedClients.TryGetValue(cacheKey, out var cachedClient))
        {
            return cachedClient;
        }

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(baseUrl);
        var client = new JsonClient(httpClient, jsonOptions);
        CachedClients.TryAdd(cacheKey, client);

        return client;
    }

    private async Task DrainQueuesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var queueName = _monitor.NextQueueName();
            if (queueName.HasValue)
            {
                try
                {
                    if (_monitorQueueMappings.TryGetValue(queueName, out var webRequest))
                    {
                        var (baseUrl, hmacSecret) =
                            WorkerConstants.Queues.QueueDeliveryApiEndpoints[queueName](_settings);
                        var apiClient = CreateApiEndpointClient("queues", _httpClientFactory, _jsonOptions, baseUrl);
                        await apiClient.PostAsync(webRequest,
                            req => req.SetHMACAuth(webRequest, hmacSecret),
                            cancellationToken);
                        _logger.LogDebug("Drained messages for queue: {Queue}", queueName);
                    }
                }
                catch (TaskCanceledException)
                {
                    //Ignore and continue
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to drain messages for queue: {Queue}", queueName);
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, cancellationToken);
            }
        }
    }

    private async Task DrainMessageBusTopicsAsync(CancellationToken cancellationToken)
    {
        var subscribers = _settings.GetEventNotificationSubscriberHosts();
        while (!cancellationToken.IsCancellationRequested)
        {
            var topicName = _monitor.NextTopicName();
            if (topicName.HasValue)
            {
                try
                {
                    if (_monitorMessageBusTopicMappings.TryGetValue(topicName, out var webRequest))
                    {
                        foreach (var subscriber in subscribers)
                        {
                            var apiClient = CreateApiEndpointClient("topics", _httpClientFactory, _jsonOptions,
                                subscriber.BaseUrl);
                            await apiClient.PostAsync(webRequest,
                                req => req.SetHMACAuth(webRequest, subscriber.HmacSecret),
                                cancellationToken);
                            _logger.LogDebug("Drained messages on bus topic: {Topic}, for: {Subscriber}", topicName,
                                subscriber.Id);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    //Ignore and continue
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to drain messages for bus topic: {Topic}", topicName);
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(CheckInterval, cancellationToken);
            }
        }
    }
}

#endif