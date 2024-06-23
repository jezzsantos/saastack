using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Events.Shared.Subscriptions;
using Domain.Interfaces;
using Domain.Shared.Organizations;
using Created = Domain.Events.Shared.Subscriptions.Created;
using Deleted = Domain.Events.Shared.Images.Deleted;

namespace OrganizationsApplication;

partial class OrganizationsApplication
{
    public async Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken)
    {
        var name =
            $"{domainEvent.UserProfile.FirstName}{(domainEvent.UserProfile.LastName.HasValue() ? " " + domainEvent.UserProfile.LastName : string.Empty)}";
        var organization = await CreateOrganizationInternalAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.Classification, name, OrganizationOwnership.Personal, cancellationToken);
        if (organization.IsFailure)
        {
            return organization.Error;
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> HandleImageDeletedAsync(ICallerContext caller, Deleted domainEvent,
        CancellationToken cancellationToken)
    {
        return await HandleDeleteAvatarAsync(caller, domainEvent.RootId.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> HandleSubscriptionCreatedAsync(ICallerContext caller,
        Created domainEvent, CancellationToken cancellationToken)
    {
        return await HandleCreatedSubscriptionAsync(caller,
            domainEvent.RootId.ToId(), domainEvent.OwningEntityId.ToId(), domainEvent.BuyerId.ToId(),
            cancellationToken);
    }

    public async Task<Result<Error>> HandleSubscriptionTransferredAsync(ICallerContext caller,
        SubscriptionTransferred domainEvent, CancellationToken cancellationToken)
    {
        return await HandleTransferSubscriptionAsync(caller, domainEvent.OwningEntityId.ToId(),
            domainEvent.FromBuyerId.ToId(), domainEvent.ToBuyerId.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> HandleEndUserMembershipAddedAsync(ICallerContext caller,
        MembershipAdded domainEvent, CancellationToken cancellationToken)
    {
        return await AddMembershipInternalAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.OrganizationId.ToId(), cancellationToken);
    }

    public async Task<Result<Error>> HandleEndUserMembershipRemovedAsync(ICallerContext caller,
        MembershipRemoved domainEvent, CancellationToken cancellationToken)
    {
        return await RemoveMembershipInternalAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.OrganizationId.ToId(), cancellationToken);
    }

    private async Task<Result<Error>> HandleCreatedSubscriptionAsync(ICallerContext caller, Identifier subscriptionId,
        Identifier organizationId, Identifier billingSubscriberId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var subscribed = org.SubscribeBilling(subscriptionId, billingSubscriberId);
        if (subscribed.IsFailure)
        {
            return subscribed.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Organization {Id} has subscribed to billing with {Subscriber}", org.Id, billingSubscriberId);

        return Result.Ok;
    }

    private async Task<Result<Error>> HandleTransferSubscriptionAsync(ICallerContext caller,
        Identifier organizationId, Identifier transfererId, Identifier transfereeId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var transferred = org.TransferBillingSubscriber(transfererId, transfereeId);
        if (transferred.IsFailure)
        {
            return transferred.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(),
            "Organization {Id} had its subscription owner transferred from {From}, to {To}", org.Id, transfererId,
            transfereeId);

        return Result.Ok;
    }

    private async Task<Result<Error>> HandleDeleteAvatarAsync(ICallerContext caller, Identifier imageId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByAvatarIdAsync(imageId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Result.Ok;
        }

        var org = retrieved.Value.Value;
        var deleted = org.ForceRemoveAvatar(CallerConstants.ServiceClientAccountUserId.ToId());
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Organization {Id} avatar was removed", org.Id);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged,
            org.ToUsageEvent(caller));

        return Result.Ok;
    }

    private async Task<Result<Error>> RemoveMembershipInternalAsync(ICallerContext caller, Identifier userId,
        Identifier organizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId, cancellationToken);
        if (retrieved.IsFailure)
        {
            //Note: this may occur after an organization is deleted, and the owner removed
            if (retrieved.Error.Is(ErrorCode.EntityDeleted))
            {
                _recorder.TraceInformation(caller.ToCall(), "Already deleted organization {Id}", organizationId);
                return Result.Ok;
            }

            return retrieved.Error;
        }

        var org = retrieved.Value;
        var removed = org.RemoveMembership(userId);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Removed organization {Id} membership for {User}", org.Id, userId);

        return Result.Ok;
    }

    private async Task<Result<Error>> AddMembershipInternalAsync(ICallerContext caller, Identifier userId,
        Identifier organizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var added = org.AddMembership(userId);
        if (added.IsFailure)
        {
            return added.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Added organization {Id} membership for {User}", org.Id,
            userId);

        return Result.Ok;
    }
}