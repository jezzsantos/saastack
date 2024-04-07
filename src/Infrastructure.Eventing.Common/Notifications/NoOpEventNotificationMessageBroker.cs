using Application.Common.Extensions;
using Common;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;

namespace Infrastructure.Eventing.Common.Notifications;

/// <summary>
///     Provides a <see cref="IEventNotificationMessageBroker" /> that does nothing.
/// </summary>
public class NoOpEventNotificationMessageBroker : IEventNotificationMessageBroker
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IRecorder _recorder;

    public NoOpEventNotificationMessageBroker(IRecorder recorder, ICallerContextFactory callerContextFactory)
    {
        _recorder = recorder;
        _callerContextFactory = callerContextFactory;
    }

    public async Task<Result<Error>> PublishAsync(IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var typeName = integrationEvent.GetType().FullName ?? integrationEvent.GetType().Name;
        _recorder.TraceDebug(_callerContextFactory.Create().ToCall(),
            "Integration event {Type} for aggregate {Id} was published.", typeName, integrationEvent.RootId);
        return Result.Ok;
    }
}