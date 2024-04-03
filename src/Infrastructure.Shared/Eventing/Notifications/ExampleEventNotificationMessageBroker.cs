using Application.Common.Extensions;
using Common;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using Integration.Events.Shared.EndUsers;

namespace Infrastructure.Shared.Eventing.Notifications;

/// <summary>
///     Provides an example message broker that relays integration events to external systems,
///     as eventually consistent with this process
/// </summary>
public class ExampleEventNotificationMessageBroker : IEventNotificationMessageBroker
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IRecorder _recorder;

    public ExampleEventNotificationMessageBroker(IRecorder recorder, ICallerContextFactory callerContextFactory)
    {
        _recorder = recorder;
        _callerContextFactory = callerContextFactory;
    }

    public async Task<Result<Error>> PublishAsync(IIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        switch (integrationEvent)
        {
            case PersonRegistered registered:
                await Task.CompletedTask;
                _recorder.TraceDebug(_callerContextFactory.Create().ToCall(),
                    "User {Id} was registered with username {Username}",
                    registered.RootId, registered.Username);
                return Result.Ok;

            default:
                return Result.Ok;
        }
    }
}