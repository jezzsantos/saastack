using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Infrastructure.Persistence.AWS.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.Persistence.AWS.ApplicationServices;

/// <summary>
///     Provides a message bus store for AWS SNS service.
///     Note: ContentDeDuplication is turned ON, as message JSON are expected to be unique (by MessageId) even for messages
///     with the same properties
///     Note: Subscriptions to these FIFO topics will be:
///     In Production: lambda functions for each of the listening subscribers
///     In TESTINGONLY: SQS FIFO queues for each of the listening subscribers
/// </summary>
[UsedImplicitly]
public class AWSSNSMessageBusStore : IMessageBusStore
{
    private readonly IAmazonCloudWatch _cloudWatchServiceClient;
    private readonly Dictionary<string, string> _knownTopicArns;
    private readonly AWSSNSMessageBusStoreOptions _options;
    private readonly AWSSQSQueueStore? _queueSubscriber;
    private readonly IRecorder _recorder;
    private readonly IAmazonSimpleNotificationService _serviceClient;

    public static AWSSNSMessageBusStore Create(IRecorder recorder, IConfigurationSettings settings,
        AWSSNSMessageBusStoreOptions options)
    {
        var (credentials, regionEndpoint) = settings.GetConnection();
        if (regionEndpoint.Exists())
        {
            var remoteClient = new AmazonSimpleNotificationServiceClient(credentials, regionEndpoint);
            var remoteCloudWatchClient = new AmazonCloudWatchClient(credentials, regionEndpoint);
            return new AWSSNSMessageBusStore(recorder, settings, remoteClient, remoteCloudWatchClient, options);
        }

        var localStackClient = new AmazonSimpleNotificationServiceClient(credentials,
            new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = AWSConstants.LocalStackServiceUrl,
                AuthenticationRegion = RegionEndpoint.USEast1.SystemName
            });
        var localCloudWatchClient = new AmazonCloudWatchClient(credentials, new AmazonCloudWatchConfig
        {
            ServiceURL = AWSConstants.LocalStackServiceUrl,
            AuthenticationRegion = RegionEndpoint.USEast1.SystemName
        });

        return new AWSSNSMessageBusStore(recorder, settings, localStackClient, localCloudWatchClient, options);
    }

    private AWSSNSMessageBusStore(IRecorder recorder, IConfigurationSettings settings,
        IAmazonSimpleNotificationService serviceClient,
        IAmazonCloudWatch cloudWatchServiceClient, AWSSNSMessageBusStoreOptions options)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _cloudWatchServiceClient = cloudWatchServiceClient;
        _options = options;
        _knownTopicArns = new Dictionary<string, string>();
        _queueSubscriber = options.Type == SubscriberType.Queue
            ? AWSSQSQueueStore.Create(recorder, settings)
            : null;
    }

#if TESTINGONLY
    public async Task<Result<long, Error>> CountAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.AWSSNSMessageBusStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter((string)nameof(subscriptionName),
            Resources.AWSSNSMessageBusStore_MissingSubscriptionName);

        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();

        var count = await _cloudWatchServiceClient.GetMetricDataAsync(new GetMetricDataRequest
        {
            StartTimeUtc = DateTime.UtcNow.AddMonths(-1),
            EndTimeUtc = DateTime.UtcNow,
            MetricDataQueries =
            [
                new MetricDataQuery
                {
                    Id = "m1",
                    MetricStat = new MetricStat
                    {
                        Metric = new Metric
                        {
                            MetricName = "NumberOfMessagesPublished",
                            Namespace = "AWS/SNS",
                            Dimensions =
                            [
                                new Dimension
                                {
                                    Name = "TopicName",
                                    Value = sanitizedTopicName
                                }
                            ]
                        },
                        Period = 86400,
                        Stat = "Sum"
                    }
                },
                new MetricDataQuery
                {
                    Id = "m2",
                    MetricStat = new MetricStat
                    {
                        Metric = new Metric
                        {
                            MetricName = "NumberOfNotificationsDelivered",
                            Namespace = "AWS/SNS",
                            Dimensions =
                            [
                                new Dimension
                                {
                                    Name = "TopicName",
                                    Value = sanitizedTopicName
                                }
                            ]
                        },
                        Period = 86400,
                        Stat = "Sum"
                    }
                }
            ]
        }, cancellationToken);

        //var published = (long)count.MetricDataResults[0].Values.FirstOrDefault();
        var delivered = (long)count.MetricDataResults[1].Values.FirstOrDefault();

        return delivered;
    }
#endif

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(string topicName, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter(nameof(topicName),
            Resources.AWSSNSMessageBusStore_MissingTopicName);

        var topicArn = await GetTopicArnAsync(topicName);
        if (!topicArn.HasValue)
        {
            return Result.Ok;
        }

        try
        {
            await _serviceClient.DeleteTopicAsync(topicArn, cancellationToken);
        }
        catch (Exception ex)
        {
            _recorder.Crash(null, CrashLevel.NonCritical, ex, "Failed to delete topic: {Topic}", topicArn);
            return ex.ToError(ErrorCode.Unexpected);
        }

        _knownTopicArns.Remove(topicName);

        return Result.Ok;
    }
#endif

#if TESTINGONLY
    public async Task<Result<bool, Error>> ReceiveSingleAsync(string topicName, string subscriptionName,
        Func<string, CancellationToken, Task<Result<Error>>> messageHandlerAsync,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.AWSSNSMessageBusStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter((string)nameof(subscriptionName),
            Resources.AWSSNSMessageBusStore_MissingSubscriptionName);
        ArgumentNullException.ThrowIfNull(messageHandlerAsync);

        topicName.SanitizeAndValidateTopicName();
        subscriptionName.SanitizeAndValidateSubscriptionName(_options);

        if (_options.Type == SubscriberType.Lambda)
        {
            return false; // Not supported
        }

        if (_queueSubscriber.NotExists())
        {
            return false; // Not supported
        }

        try
        {
            if (!Arn.TryParse(subscriptionName, out var arn))
            {
                return false;
            }

            var queueName = arn.Resource.Replace(".fifo", string.Empty);
            var received =
                await _queueSubscriber!.PopSingleAsync(queueName, messageHandlerAsync, cancellationToken);
            if (received.IsFailure)
            {
                return received.Error;
            }

            return received.Value;
        }
        catch (Exception ex)
        {
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to handle last message from topic: {Topic} for subscription: {Subscription}",
                topicName, subscriptionName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }
#endif

    public async Task<Result<Error>> SendAsync(string topicName, string message, CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.AWSSNSMessageBusStore_MissingTopicName);
        message.ThrowIfNotValuedParameter((string)nameof(message), Resources.AWSSNSMessageBusStore_MissingSentMessage);

        try
        {
            var sent = await SendMessageInternalAsync(topicName, message, cancellationToken);
            if (sent.IsFailure)
            {
                return sent.Error;
            }
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _recorder.Crash(null,
                CrashLevel.NonCritical,
                ex, "Failed to send message: {Message} to the topic: {Topic}", message, topicName);
            return ex.ToError(ErrorCode.Unexpected);
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> SubscribeAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        topicName.ThrowIfNotValuedParameter((string)nameof(topicName),
            Resources.AWSSNSMessageBusStore_MissingTopicName);
        subscriptionName.ThrowIfNotValuedParameter((string)nameof(subscriptionName),
            Resources.AWSSNSMessageBusStore_MissingSubscriptionName);

        await CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);

        return Result.Ok;
    }

    private async Task<Result<Error>> SendMessageInternalAsync(string topicName, string message,
        CancellationToken cancellationToken)
    {
        var command = async () =>
        {
            var topicArn = await GetTopicArnAsync(topicName);
            if (!topicArn.HasValue)
            {
                topicArn = await CreateTopicAsync(topicName, cancellationToken);
            }

            await _serviceClient.PublishAsync(new PublishRequest
            {
                TopicArn = topicArn,
                Message = message,
                MessageGroupId = AWSConstants.FifoGroupName
            }, cancellationToken);

            return Result.Ok;
        };

        try
        {
            return await command();
        }
        catch (NotFoundException)
        {
            await CreateTopicAsync(topicName, cancellationToken);
            return await command();
        }
    }

    private async Task<string> CreateTopicAsync(string topicName, CancellationToken cancellationToken)
    {
        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();
        var topic = await _serviceClient.CreateTopicAsync(new CreateTopicRequest
        {
            Name = $"{sanitizedTopicName}.fifo",
            Attributes =
            {
                ["FifoTopic"] = "true",
                [QueueAttributeName.ContentBasedDeduplication] = "true"
            }
        }, cancellationToken);

        var topicArn = topic.TopicArn;
        _knownTopicArns.Add(sanitizedTopicName, topicArn);
        return topicArn;
    }

    private async Task CreateSubscriptionAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        var topicArn = await CreateTopicAsync(topicName, cancellationToken);

        var sanitizedSubscriptionName = subscriptionName.SanitizeAndValidateSubscriptionName(_options);

        var protocol = _options.Type == SubscriberType.Lambda
            ? "lambda"
            : "sqs";
        await _serviceClient.SubscribeAsync(new SubscribeRequest
        {
            TopicArn = topicArn,
            Protocol = protocol,
            Endpoint = sanitizedSubscriptionName,
            Attributes = new Dictionary<string, string>
            {
                { "RawMessageDelivery", "true" }
            }
        }, cancellationToken);
    }

    private async Task<Optional<string>> GetTopicArnAsync(string topicName)
    {
        var sanitizedTopicName = topicName.SanitizeAndValidateTopicName();
        if (_knownTopicArns.TryGetValue(sanitizedTopicName, out var arn))
        {
            return arn;
        }

        var topic = await _serviceClient.FindTopicAsync(sanitizedTopicName);
        if (topic.NotExists()
            || topic.TopicArn.HasNoValue())
        {
            return Optional<string>.None;
        }

        var topicArn = topic.TopicArn;
        _knownTopicArns.Add(sanitizedTopicName, topicArn);
        return topicArn;
    }
}

public record AWSSNSMessageBusStoreOptions(SubscriberType Type);

public enum SubscriberType
{
    Lambda = 0,
    Queue = 1
}