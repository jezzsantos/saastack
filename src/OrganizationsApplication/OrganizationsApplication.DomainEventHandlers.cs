using Application.Common.Extensions;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Shared.Organizations;

namespace OrganizationsApplication;

partial class OrganizationsApplication
{
    public async Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken)
    {
        var name =
            $"{domainEvent.UserProfile.FirstName}{(domainEvent.UserProfile.LastName.HasValue() ? " " + domainEvent.UserProfile.LastName : string.Empty)}";
        var organization = await CreateOrganizationInternalAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.Classification, name,
            OrganizationOwnership.Personal, cancellationToken);
        if (!organization.IsSuccessful)
        {
            return organization.Error;
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> HandleEndUserMembershipAddedAsync(ICallerContext caller,
        MembershipAdded domainEvent, CancellationToken cancellationToken)
    {
        var organization = await AddMembershipInternalAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.OrganizationId.ToId(), cancellationToken);
        if (!organization.IsSuccessful)
        {
            return organization.Error;
        }

        return Result.Ok;
    }

    public async Task<Result<Error>> HandleEndUserMembershipRemovedAsync(ICallerContext caller,
        MembershipRemoved domainEvent, CancellationToken cancellationToken)
    {
        var organization = await RemoveMembershipInternalAsync(caller, domainEvent.RootId.ToId(),
            domainEvent.OrganizationId.ToId(), cancellationToken);
        if (!organization.IsSuccessful)
        {
            return organization.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> RemoveMembershipInternalAsync(ICallerContext caller, Identifier userId,
        Identifier organizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(organizationId, cancellationToken);
        if (!retrieved.IsSuccessful)
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
        if (!removed.IsSuccessful)
        {
            return removed.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
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
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var org = retrieved.Value;
        var added = org.AddMembership(userId);
        if (!added.IsSuccessful)
        {
            return added.Error;
        }

        var saved = await _repository.SaveAsync(org, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        org = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Added organization {Id} membership for {User}", org.Id,
            userId);

        return Result.Ok;
    }
}