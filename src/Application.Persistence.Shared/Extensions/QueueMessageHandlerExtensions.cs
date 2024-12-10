using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;

namespace Application.Persistence.Shared.Extensions;

public static class QueueMessageHandlerExtensions
{
    public static async Task DrainAllQueuedMessagesAsync<TQueuedMessage>(
        this IMessageQueueStore<TQueuedMessage> repository,
        Func<TQueuedMessage, Task<Result<bool, Error>>> handler, CancellationToken cancellationToken)
        where TQueuedMessage : IQueuedMessage, new()
    {
        var found = new Result<bool, Error>(true);
        while (found.Value)
        {
            found = await repository.PopSingleAsync(OnMessageReceivedAsync, cancellationToken);
            continue;

            async Task<Result<Error>> OnMessageReceivedAsync(TQueuedMessage message, CancellationToken _)
            {
                var handled = await handler(message);
                if (handled.IsFailure)
                {
                    handled.Error.Throw();
                }

                return Result.Ok;
            }
        }
    }

    public static Result<TQueuedMessage, Error> RehydrateQueuedMessage<TQueuedMessage>(this string messageAsJson)
        where TQueuedMessage : IQueuedMessage
    {
        try
        {
            var message = messageAsJson.FromJson<TQueuedMessage>();
            if (message.NotExists())
            {
                return Error.RuleViolation(
                    Resources.QueueMessageHandlerExtensions_InvalidQueuedMessage.Format(typeof(TQueuedMessage).Name,
                        messageAsJson));
            }

            return message;
        }
        catch (Exception)
        {
            return Error.RuleViolation(
                Resources.QueueMessageHandlerExtensions_InvalidQueuedMessage.Format(typeof(TQueuedMessage).Name,
                    messageAsJson));
        }
    }
}