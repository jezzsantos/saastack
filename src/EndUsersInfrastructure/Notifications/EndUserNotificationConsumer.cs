using Common;
using Domain.Events.Shared.Organizations;
using Domain.Interfaces.Entities;
using EndUsersApplication;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Interfaces;

namespace EndUsersInfrastructure.Notifications;

public class EndUserNotificationConsumer : IDomainEventNotificationConsumer
{
    private readonly ICallerContextFactory _callerContextFactory;
    private readonly IEndUsersApplication _endUsersApplication;
    private readonly IInvitationsApplication _invitationsApplication;

    public EndUserNotificationConsumer(ICallerContextFactory callerContextFactory,
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
            case Created created:
                return await _endUsersApplication.HandleOrganizationCreatedAsync(_callerContextFactory.Create(),
                    created, cancellationToken);

            case MembershipAdded added:
                return await _invitationsApplication.HandleOrganizationMembershipAddedAsync(
                    _callerContextFactory.Create(),
                    added, cancellationToken);

            default:
                return Result.Ok;
        }
    }
}