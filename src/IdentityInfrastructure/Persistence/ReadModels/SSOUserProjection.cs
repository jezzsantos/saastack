using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using IdentityApplication.Persistence.ReadModels;
using IdentityDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace IdentityInfrastructure.Persistence.ReadModels;

public class SSOUserProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<SSOUser> _users;

    public SSOUserProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _users = new ReadModelProjectionStore<SSOUser>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(SSOUserRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _users.HandleCreateAsync(e.RootId.ToId(), dto =>
                    {
                        dto.UserId = e.UserId;
                        dto.ProviderName = e.ProviderName;
                    },
                    cancellationToken);

            case TokensUpdated e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto =>
                {
                    dto.Tokens = e.Tokens;
                    dto.EmailAddress = e.EmailAddress;
                    dto.FirstName = e.FirstName;
                    dto.LastName = e.LastName;
                    dto.Timezone = e.Timezone;
                    dto.CountryCode = e.CountryCode;
                }, cancellationToken);

            default:
                return false;
        }
    }
}