using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using Common;
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
    private readonly IReadModelStore<UserProfile> _users;

    public UserProfileProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _users = new ReadModelStore<UserProfile>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _users.HandleCreateAsync(e.RootId, dto =>
                {
                    dto.Type = e.Type;
                    dto.UserId = e.UserId;
                    dto.DisplayName = e.DisplayName;
                    dto.FirstName = e.FirstName;
                    dto.LastName = e.LastName;
                }, cancellationToken);

            case NameChanged e:
                return await _users.HandleUpdateAsync(e.RootId, dto =>
                {
                    dto.FirstName = e.FirstName;
                    dto.LastName = e.LastName;
                }, cancellationToken);

            case DisplayNameChanged e:
                return await _users.HandleUpdateAsync(e.RootId, dto => { dto.DisplayName = e.DisplayName; },
                    cancellationToken);

            case EmailAddressChanged e:
                return await _users.HandleUpdateAsync(e.RootId, dto => { dto.EmailAddress = e.EmailAddress; },
                    cancellationToken);

            case PhoneNumberChanged e:
                return await _users.HandleUpdateAsync(e.RootId, dto => { dto.PhoneNumber = e.Number; },
                    cancellationToken);

            case TimezoneChanged e:
                return await _users.HandleUpdateAsync(e.RootId, dto => { dto.Timezone = e.Timezone; },
                    cancellationToken);

            case ContactAddressChanged e:
                return await _users.HandleUpdateAsync(e.RootId, dto => { dto.CountryCode = e.CountryCode; },
                    cancellationToken);

            case AvatarAdded e:
                return await _users.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.AvatarImageId = e.AvatarId;
                        dto.AvatarUrl = e.AvatarUrl;
                    },
                    cancellationToken);

            case AvatarRemoved e:
                return await _users.HandleUpdateAsync(e.RootId, dto =>
                    {
                        dto.AvatarImageId = Optional<string>.None;
                        dto.AvatarUrl = Optional<string>.None;
                    },
                    cancellationToken);

            case DefaultOrganizationChanged e:
                return await _users.HandleUpdateAsync(e.RootId,
                    dto => { dto.DefaultOrganizationId = e.ToOrganizationId; },
                    cancellationToken);

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(UserProfileRoot);
}