using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
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
    private readonly IReadModelStore<SSOUser> _users;

    public SSOUserProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _users = new ReadModelStore<SSOUser>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _users.HandleCreateAsync(e.RootId, dto =>
                    {
                        dto.UserId = e.UserId;
                        dto.ProviderName = e.ProviderName;
                        dto.ProviderUId = Optional<string>.None;
                        dto.EmailAddress = Optional<string>.None;
                        dto.FirstName = Optional<string>.None;
                        dto.LastName = Optional<string>.None;
                        dto.Timezone = Optional<string>.None;
                        dto.Locale = Optional<string>.None;
                        dto.CountryCode = Optional<string>.None;
                    },
                    cancellationToken);

            case DetailsChanged e:
                return await _users.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.ProviderUId = e.ProviderUId;
                    dto.EmailAddress = e.EmailAddress;
                    dto.FirstName = e.FirstName;
                    dto.LastName = e.LastName;
                    dto.Timezone = e.Timezone;
                    dto.Locale = e.Locale;
                    dto.CountryCode = e.CountryCode;
                }, cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(SSOUserRoot);
}