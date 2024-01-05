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

namespace EndUsersInfrastructure.Persistence.ReadModels;

public class EndUserProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<EndUser> _users;

    public EndUserProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _users = new ReadModelProjectionStore<EndUser>(recorder, domainFactory, store);
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
                    dto.Roles = Roles.Create(e.Roles).Value;
                    dto.FeatureLevels = FeatureLevels.Create(e.FeatureLevels).Value;
                }, cancellationToken);

            default:
                return false;
        }
    }
}