using Common;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces.Entities;
using EndUsersApplication;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;

namespace EndUsersInfrastructure.Notifications;

public class OrganizationNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IEndUsersApplication _endUsersApplication;
    private readonly IInvitationsApplication _invitationsApplication;

    public OrganizationNotificationConsumer(ICallerContextFactory callerContextFactory,
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

            default:
                return Result.Ok;
        }
    }
}