using Common;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Integration.Events.Shared.EndUsers;

namespace EndUsersInfrastructure.Notifications;

/// <summary>
///     Provides an example translator of domain events that should be published as integration events
/// </summary>
public sealed class
    EndUserIntegrationEventNotificationTranslator<TAggregateRoot> : IIntegrationEventNotificationTranslator
    where TAggregateRoot : IEventingAggregateRoot
{
    public Type RootAggregateType => typeof(TAggregateRoot);

    public async Task<Result<Optional<IIntegrationEvent>, Error>> TranslateAsync(IDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        switch (domainEvent)
        {
            case Registered registered:
                return new PersonRegistered(registered.RootId)
                {
                    Features = registered.Features,
                    Roles = registered.Roles,
                    Username = registered.Username ?? string.Empty,
                    UserProfile = registered.UserProfile
                }.ToOptional<IIntegrationEvent>();
        }

        return Optional<IIntegrationEvent>.None;
    }
}