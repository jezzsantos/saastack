using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Shared;
using EndUsersDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using EndUser = EndUsersApplication.Persistence.ReadModels.EndUser;
using Membership = EndUsersApplication.Persistence.ReadModels.Membership;

namespace EndUsersInfrastructure.Persistence.ReadModels;

public class EndUserProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<Membership> _memberships;
    private readonly IReadModelProjectionStore<EndUser> _users;

    public EndUserProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _users = new ReadModelProjectionStore<EndUser>(recorder, domainFactory, store);
        _memberships = new ReadModelProjectionStore<Membership>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(EndUserRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Events.Created e:
                return await _users.HandleCreateAsync(e.RootId.ToId(), dto =>
                {
                    dto.Classification = e.Classification;
                    dto.Access = e.Access;
                    dto.Status = e.Status;
                }, cancellationToken);

            case Events.Registered e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto =>
                {
                    dto.Classification = e.Classification;
                    dto.Access = e.Access;
                    dto.Status = e.Status;
                    dto.Username = e.Username;
                    dto.Roles = Roles.Create(e.Roles.ToArray()).Value;
                    dto.Features = Features.Create(e.Features.ToArray()).Value;
                }, cancellationToken);

            case Events.MembershipAdded e:
                return await _memberships.HandleCreateAsync(e.MembershipId.ToId(), dto =>
                {
                    dto.IsDefault = e.IsDefault;
                    dto.UserId = e.RootId;
                    dto.OrganizationId = e.OrganizationId;
                    dto.Roles = Roles.Create(e.Roles.ToArray()).Value;
                    dto.Features = Features.Create(e.Features.ToArray()).Value;
                }, cancellationToken);

            case Events.MembershipDefaultChanged e:
            {
                var from = await _memberships.HandleUpdateAsync(e.FromMembershipId.ToId(),
                    dto => { dto.IsDefault = false; }, cancellationToken);
                if (!from.IsSuccessful)
                {
                    return from.Error;
                }

                var to = await _memberships.HandleUpdateAsync(e.ToMembershipId.ToId(),
                    dto => { dto.IsDefault = true; }, cancellationToken);
                if (!to.IsSuccessful)
                {
                    return to.Error;
                }

                return to;
            }

            case Events.MembershipRoleAssigned e:
                return await _memberships.HandleUpdateAsync(e.RootId.ToId(), dto =>
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

            case Events.MembershipFeatureAssigned e:
                return await _memberships.HandleUpdateAsync(e.RootId.ToId(), dto =>
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

            case Events.PlatformRoleAssigned e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto =>
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

            case Events.PlatformFeatureAssigned e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto =>
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

            default:
                return false;
        }
    }
}