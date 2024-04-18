using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.UserProfiles;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using UserProfilesDomain;
using UserProfile = UserProfilesApplication.Persistence.ReadModels.UserProfile;

namespace UserProfilesInfrastructure.Persistence.ReadModels;

public class UserProfileProjection : IReadModelProjection
{
    private readonly IReadModelProjectionStore<UserProfile> _users;

    public UserProfileProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _users = new ReadModelProjectionStore<UserProfile>(recorder, domainFactory, store);
    }

    public Type RootAggregateType => typeof(UserProfileRoot);

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _users.HandleCreateAsync(e.RootId.ToId(), dto =>
                {
                    dto.Type = e.Type;
                    dto.UserId = e.UserId;
                    dto.DisplayName = e.DisplayName;
                    dto.FirstName = e.FirstName;
                    dto.LastName = e.LastName;
                }, cancellationToken);

            case NameChanged e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto =>
                {
                    dto.FirstName = e.FirstName;
                    dto.LastName = e.LastName;
                }, cancellationToken);

            case DisplayNameChanged e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto => { dto.DisplayName = e.DisplayName; },
                    cancellationToken);

            case EmailAddressChanged e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto => { dto.EmailAddress = e.EmailAddress; },
                    cancellationToken);

            case PhoneNumberChanged e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto => { dto.PhoneNumber = e.Number; },
                    cancellationToken);

            case TimezoneChanged e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto => { dto.Timezone = e.Timezone; },
                    cancellationToken);

            case ContactAddressChanged e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto => { dto.CountryCode = e.CountryCode; },
                    cancellationToken);

            case AvatarAdded e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto =>
                    {
                        dto.AvatarId = e.AvatarId;
                        dto.AvatarUrl = e.AvatarUrl;
                    },
                    cancellationToken);

            case AvatarRemoved e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(), dto =>
                    {
                        dto.AvatarId = Optional<string>.None;
                        dto.AvatarUrl = Optional<string>.None;
                    },
                    cancellationToken);

            case DefaultOrganizationChanged e:
                return await _users.HandleUpdateAsync(e.RootId.ToId(),
                    dto => { dto.DefaultOrganizationId = e.ToOrganizationId; },
                    cancellationToken);

            default:
                return false;
        }
    }
}