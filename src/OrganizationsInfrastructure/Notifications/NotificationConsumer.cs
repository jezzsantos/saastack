using Common;
using Domain.Events.Shared.EndUsers;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using OrganizationsApplication;
using SubscriptionCreated = Domain.Events.Shared.Subscriptions.Created;
using ImageDeleted = Domain.Events.Shared.Images.Deleted;

namespace OrganizationsInfrastructure.Notifications;

public class NotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IOrganizationsApplication _organizationsApplication;

    public NotificationConsumer(ICallerContextFactory callerContextFactory,
        IOrganizationsApplication organizationsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _organizationsApplication = organizationsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Registered registered:
                return await _organizationsApplication.HandleEndUserRegisteredAsync(_callerContextFactory.Create(),
                    registered, cancellationToken);

            case MembershipAdded added:
                return await _organizationsApplication.HandleEndUserMembershipAddedAsync(
                    _callerContextFactory.Create(), added, cancellationToken);

            case MembershipRemoved removed:
                return await _organizationsApplication.HandleEndUserMembershipRemovedAsync(
                    _callerContextFactory.Create(), removed, cancellationToken);

            case ImageDeleted deleted:
                return await _organizationsApplication.HandleImageDeletedAsync(_callerContextFactory.Create(),
                    deleted, cancellationToken);

            case SubscriptionCreated created:
                return await _organizationsApplication.HandleSubscriptionCreatedAsync(
                    _callerContextFactory.Create(), created, cancellationToken);

            case SubscriptionTransferred transferred:
                return await _organizationsApplication.HandleSubscriptionTransferredAsync(
                    _callerContextFactory.Create(), transferred, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}