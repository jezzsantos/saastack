using Common;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using JetBrains.Annotations;

namespace IdentityDomain;

/// <summary>
///     Note: This aggregate is used to store the SSO auth details for a specific SSO provider.
///     It might be updated every time the external provider authenticates the user.
///     It is also updated, whenever the info about the user changes in the external provider,
///     which should not be that often.
/// </summary>
public sealed class SSOUserRoot : AggregateRootBase
{
    public static Result<SSOUserRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        string providerName, Identifier userId)
    {
        var root = new SSOUserRoot(recorder, idFactory);
        root.RaiseCreateEvent(IdentityDomain.Events.SSOUsers.Created(root.Id, providerName, userId));
        return root;
    }

    private SSOUserRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(
        recorder, idFactory)
    {
    }

    private SSOUserRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Optional<Address> Address { get; private set; }

    public Optional<EmailAddress> EmailAddress { get; private set; }

    public Optional<PersonName> Name { get; private set; }

    public Optional<string> ProviderName { get; private set; }

    public Optional<string> ProviderUId { get; private set; }

    public Optional<Timezone> Timezone { get; private set; }

    public Optional<Locale> Locale { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    [UsedImplicitly]
    public static AggregateRootFactory<SSOUserRoot> Rehydrate()
    {
        return (identifier, container, _) => new SSOUserRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(),
            identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                ProviderName = created.ProviderName;
                UserId = created.UserId.ToId();
                return Result.Ok;
            }

            case DetailsChanged changed:
            {
                ProviderUId = changed.ProviderUId;
                var emailAddress = Domain.Shared.EmailAddress.Create(changed.EmailAddress);
                if (emailAddress.IsFailure)
                {
                    return emailAddress.Error;
                }

                EmailAddress = emailAddress.Value;
                var name = PersonName.Create(changed.FirstName, changed.LastName);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;
                var timezone = Domain.Shared.Timezone.Create(changed.Timezone);
                if (timezone.IsFailure)
                {
                    return timezone.Error;
                }

                Timezone = timezone.Value;
                var locale = Domain.Shared.Locale.Create(changed.Locale);
                if (locale.IsFailure)
                {
                    return locale.Error;
                }

                Locale = locale.Value;
                var address = Domain.Shared.Address.Create(CountryCodes.FindOrDefault(changed.CountryCode));
                if (address.IsFailure)
                {
                    return address.Error;
                }

                Address = address.Value;
                Recorder.TraceDebug(null, "User {Id} have changed their details", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> ChangeDetails(string providerUniqueId, EmailAddress emailAddress,
        PersonName name, Timezone timezone, Locale locale, Address address)
    {
        if (DetailsHaveChanged())
        {
            var detailsUpdated = RaiseChangeEvent(
                IdentityDomain.Events.SSOUsers.DetailsChanged(Id, providerUniqueId, emailAddress, name, timezone,
                    locale, address));
            if (detailsUpdated.IsFailure)
            {
                return detailsUpdated.Error;
            }
        }

        return Result.Ok;

        bool DetailsHaveChanged()
        {
            return providerUniqueId != ProviderUId
                   || emailAddress != EmailAddress
                   || name != Name
                   || locale != Locale
                   || timezone != Timezone
                   || address != Address;
        }
    }

    public Result<Error> ViewUser(Identifier viewerId)
    {
        if (!IsOwner(viewerId))
        {
            return Error.RoleViolation(Resources.SSOUserRoot_NotOwner);
        }

        return Result.Ok;
    }

    private bool IsOwner(Identifier userId)
    {
        return userId == UserId;
    }
}