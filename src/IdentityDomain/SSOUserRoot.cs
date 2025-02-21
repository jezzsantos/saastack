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

    public Optional<SSOAuthTokens> Tokens { get; private set; }

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

            case TokensChanged changed:
            {
                var tokens = SSOAuthTokens.Create(changed.Tokens);
                if (tokens.IsFailure)
                {
                    return tokens.Error;
                }

                Tokens = tokens.Value;
                Recorder.TraceDebug(null, "User {Id} has changed their tokens", Id);
                return Result.Ok;
            }

            case DetailsAdded added:
            {
                ProviderUId = added.ProviderUId;
                var emailAddress = Domain.Shared.EmailAddress.Create(added.EmailAddress);
                if (emailAddress.IsFailure)
                {
                    return emailAddress.Error;
                }

                EmailAddress = emailAddress.Value;
                var name = PersonName.Create(added.FirstName, added.LastName);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                Name = name.Value;
                var timezone = Domain.Shared.Timezone.Create(added.Timezone);
                if (timezone.IsFailure)
                {
                    return timezone.Error;
                }

                Timezone = timezone.Value;
                var address = Domain.Shared.Address.Create(CountryCodes.FindOrDefault(added.CountryCode));
                if (address.IsFailure)
                {
                    return address.Error;
                }

                Address = address.Value;
                Recorder.TraceDebug(null, "User {Id} has added their details", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> AddDetails(SSOAuthTokens tokens, string uId, EmailAddress emailAddress,
        PersonName name, Timezone timezone, Address address)
    {
        var detailsUpdated = RaiseChangeEvent(
            IdentityDomain.Events.SSOUsers.DetailsAdded(Id, uId, emailAddress, name, timezone,
                address));
        if (detailsUpdated.IsFailure)
        {
            return detailsUpdated.Error;
        }

        return RaiseChangeEvent(IdentityDomain.Events.SSOUsers.TokensChanged(Id, tokens));
    }

    public Result<Error> ChangeTokens(Identifier modifierId, SSOAuthTokens tokens)
    {
        if (!IsOwner(modifierId))
        {
            return Error.RoleViolation(Resources.SSOUserRoot_NotOwner);
        }

        return RaiseChangeEvent(IdentityDomain.Events.SSOUsers.TokensChanged(Id, tokens));
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