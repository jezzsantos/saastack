using Common.Extensions;
using Infrastructure.External.Persistence.AWS.ApplicationServices;

namespace Infrastructure.External.Persistence.AWS.Extensions;

public static class ValidationExtensions
{
    /// <summary>
    ///     Sanitizes and validates the queue name.
    ///     Limitations here:
    ///     <see
    ///         href="https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-fifo-queue-message-identifiers.html" />
    /// </summary>
    public static string SanitizeAndValidateQueueName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateQueueName(lowercased);

        return lowercased;
    }

    /// <summary>
    ///     Sanitizes and validates the message bus subscription name.
    ///     Which are ARNs to lambda functions, for example:
    ///     arn:aws:lambda:ap-southeast-2:218440262963:function:ApiHost1
    /// </summary>
    public static string SanitizeAndValidateSubscriptionName(this string name, AWSSNSMessageBusStoreOptions options)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateSubscriptionName(lowercased, options);

        return lowercased;
    }

    /// <summary>
    ///     Sanitizes and validates the message bus topic name, for a FIFO topic.
    ///     Limitations here:
    ///     <see
    ///         href="https://aws.amazon.com/sns/faqs/#:~:text=Topic%20names%20are%20limited%20to,can%20reuse%20the%20topic%20name." />
    /// </summary>
    public static string SanitizeAndValidateTopicName(this string name)
    {
        var lowercased = name.ToLowerInvariant();
        ValidateTopicName(lowercased);

        return lowercased;
    }

    private static void ValidateQueueName(string name)
    {
        if (!name.IsMatchWith(AWSConstants.SQSQueueNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidQueueName.Format(name));
        }
    }

    private static void ValidateTopicName(string name)
    {
        if (!name.IsMatchWith(AWSConstants.SNSTopicNameValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidMessageBusTopicName.Format(name));
        }
    }

    private static void ValidateSubscriptionName(string name, AWSSNSMessageBusStoreOptions options)
    {
        switch (options.Type)
        {
            case SubscriberType.Lambda:
                ValidateLambdaSubscriptionName(name);
                break;

            case SubscriberType.Queue:
                ValidateQueueSubscriptionName(name);
                break;

            default:
                throw new ArgumentOutOfRangeException(options.Type.ToString());
        }
    }

    private static void ValidateQueueSubscriptionName(string name)
    {
        if (!name.IsMatchWith(AWSConstants.QueueArnValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidMessageBusSubscriptionName.Format(name));
        }
    }

    private static void ValidateLambdaSubscriptionName(string name)
    {
        if (!name.IsMatchWith(AWSConstants.LambdaArnValidationExpression))
        {
            throw new ArgumentOutOfRangeException(
                Resources.ValidationExtensions_InvalidMessageBusSubscriptionName.Format(name));
        }
    }
}