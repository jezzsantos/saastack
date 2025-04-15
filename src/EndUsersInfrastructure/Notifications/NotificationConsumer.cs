using Common;
using Domain.Events.Shared.Organizations;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces.Entities;
using EndUsersApplication;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;
using Created = Domain.Events.Shared.Organizations.Created;
using Deleted = Domain.Events.Shared.Organizations.Deleted;

namespace EndUsersInfrastructure.Notifications;

public class NotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IEndUsersApplication _endUsersApplication;
    private readonly IInvitationsApplication _invitationsApplication;

    public NotificationConsumer(ICallerContextFactory callerContextFactory,
        IEndUsersApplication endUsersApplication, IInvitationsApplication invitationsApplication)
    {
        _callerContextFactory = callerContextFactory;
        _endUsersApplication = endUsersApplication;
        _invitationsApplication = invitationsApplication;
    }

    public async Task<Result<Error>> NotifyAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        switch (domainEvent)
        {
            case Deleted deleted:
                return await _endUsersApplication.HandleOrganizationDeletedAsync(_callerContextFactory.Create(),
                    deleted, cancellationToken);

            case Created created:
                return await _endUsersApplication.HandleOrganizationCreatedAsync(_callerContextFactory.Create(),
                    created, cancellationToken);

            case MemberInvited added:
                return await _invitationsApplication.HandleOrganizationMemberInvitedAsync(
                    _callerContextFactory.Create(), added, cancellationToken);

            case MemberUnInvited removed:
                return await _invitationsApplication.HandleOrganizationMemberUnInvitedAsync(
                    _callerContextFactory.Create(), removed, cancellationToken);

            case RoleAssigned assigned:
                return await _endUsersApplication.HandleOrganizationRoleAssignedAsync(
                    _callerContextFactory.Create(), assigned, cancellationToken);

            case RoleUnassigned unassigned:
                return await _endUsersApplication.HandleOrganizationRoleUnassignedAsync(
                    _callerContextFactory.Create(), unassigned, cancellationToken);

            case SubscriptionPlanChanged changed:
                return await _endUsersApplication.HandleSubscriptionPlanChangedAsync(
                    _callerContextFactory.Create(), changed, cancellationToken);

            case SubscriptionTransferred transferred:
                return await _endUsersApplication.HandleSubscriptionTransferredAsync(
                    _callerContextFactory.Create(), transferred, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}