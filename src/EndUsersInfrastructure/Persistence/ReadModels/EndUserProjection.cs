using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.EndUsers;
using EndUsersApplication.Persistence.ReadModels;
using EndUsersDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using EndUser = EndUsersApplication.Persistence.ReadModels.EndUser;
using Membership = EndUsersApplication.Persistence.ReadModels.Membership;
using Tasks = Application.Persistence.Common.Extensions.Tasks;

namespace EndUsersInfrastructure.Persistence.ReadModels;

public class EndUserProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<Invitation> _invitations;
    private readonly IReadModelProjectionStore<Membership> _memberships;
    private readonly IReadModelProjectionStore<EndUser> _users;

    public EndUserProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _users = new ReadModelProjectionStore<EndUser>(recorder, domainFactory, store);
        _invitations = new ReadModelProjectionStore<Invitation>(recorder, domainFactory, store);
        _memberships = new ReadModelProjectionStore<Membership>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(EndUserRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await Tasks.WhenAllAsync(_users.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.Classification = e.Classification;
                        dto.Access = e.Access;
                        dto.Status = e.Status;
                    }, cancellationToken),
                    _invitations.HandleCreateAsync(e.RootId, dto => { dto.Status = e.Status; },
                        cancellationToken));

            case Registered e:
                return await Tasks.WhenAllAsync(_users.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.Classification = e.Classification;
                        dto.Access = e.Access;
                        dto.Status = e.Status;
                        dto.Username = e.Username;
                        dto.Roles = Roles.Create(e.Roles.ToArray()).Value;
                        dto.Features = Features.Create(e.Features.ToArray()).Value;
                    }, cancellationToken),
                    _invitations.HandleUpdateAsync(e.RootId, dto => { dto.Status = e.Status; },
                        cancellationToken));

            case MembershipAdded e:
                return e.MembershipId.HasValue()
                    ? await _memberships.HandleCreateAsync(e.MembershipId, dto =>
                    {
                        dto.IsDefault = e.IsDefault;
                        dto.UserId = e.RootId;
                        dto.OrganizationId = e.OrganizationId;
                        dto.Ownership = e.Ownership;
                        dto.Roles = Roles.Create(e.Roles.ToArray()).Value;
                        dto.Features = Features.Create(e.Features.ToArray()).Value;
                    }, cancellationToken)
                    : true;

            case MembershipRemoved e:
                return await _memberships.HandleDeleteAsync(e.MembershipId, cancellationToken);

            case DefaultMembershipChanged e:
            {
                if (e.FromMembershipId.Exists())
                {
                    var from = await _memberships.HandleUpdateAsync(e.FromMembershipId.ToId(),
                        dto => { dto.IsDefault = false; }, cancellationToken);
                    if (!from.IsSuccessful)
                    {
                        return from.Error;
                    }
                }

                var to = await _memberships.HandleUpdateAsync(e.ToMembershipId.ToId(),
                    dto => { dto.IsDefault = true; }, cancellationToken);
                if (!to.IsSuccessful)
                {
                    return to.Error;
                }

                return to;
            }

            case MembershipRoleAssigned e:
                return await _memberships.HandleUpdateAsync(e.RootId, dto =>
                {
                    var roles = dto.Roles.HasValue
                        ? dto.Roles.Value.Add(e.Role)
                        : Roles.Create(e.Role);
                    if (!roles.IsSuccessful)
                    {
                        return;
                    }

                    dto.Roles = roles.Value;
                }, cancellationToken);

            case MembershipRoleUnassigned e:
                return await _memberships.HandleUpdateAsync(e.RootId, dto =>
                {
                    var roles = dto.Roles.HasValue
                        ? dto.Roles.Value.Remove(e.Role)
                        : new Result<Roles, Error>(Roles.Empty);
                    if (!roles.IsSuccessful)
                    {
                        return;
                    }

                    dto.Roles = roles.Value;
                }, cancellationToken);

            case MembershipFeatureAssigned e:
                return await _memberships.HandleUpdateAsync(e.RootId, dto =>
                {
                    var features = dto.Features.HasValue
                        ? dto.Features.Value.Add(e.Feature)
                        : Features.Create(e.Feature);
                    if (!features.IsSuccessful)
                    {
                        return;
                    }

                    dto.Features = features.Value;
                }, cancellationToken);

            case PlatformRoleAssigned e:
                return await _users.HandleUpdateAsync(e.RootId, dto =>
                {
                    var roles = dto.Roles.HasValue
                        ? dto.Roles.Value.Add(e.Role)
                        : Roles.Create(e.Role);
                    if (!roles.IsSuccessful)
                    {
                        return;
                    }

                    dto.Roles = roles.Value;
                }, cancellationToken);

            case PlatformRoleUnassigned e:
                return await _users.HandleUpdateAsync(e.RootId, dto =>
                {
                    var roles = dto.Roles.HasValue
                        ? dto.Roles.Value.Remove(e.Role)
                        : new Result<Roles, Error>(Roles.Empty);
                    if (!roles.IsSuccessful)
                    {
                        return;
                    }

                    dto.Roles = roles.Value;
                }, cancellationToken);

            case PlatformFeatureAssigned e:
                return await _users.HandleUpdateAsync(e.RootId, dto =>
                {
                    var features = dto.Features.HasValue
                        ? dto.Features.Value.Add(e.Feature)
                        : Features.Create(e.Feature);
                    if (!features.IsSuccessful)
                    {
                        return;
                    }

                    dto.Features = features.Value;
                }, cancellationToken);

            case GuestInvitationCreated e:
                return await _invitations.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.InvitedEmailAddress = e.EmailAddress;
                    dto.Token = e.Token;
                    dto.InvitedById = e.InvitedById;
                }, cancellationToken);

            case GuestInvitationAccepted e:
                return await _invitations.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.Token = Optional<string>.None;
                    dto.Status = UserStatus.Registered;
                    dto.AcceptedAtUtc = e.AcceptedAtUtc;
                    dto.AcceptedEmailAddress = e.AcceptedEmailAddress;
                }, cancellationToken);

            default:
                return false;
        }
    }
}