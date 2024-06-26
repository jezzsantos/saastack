using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared;

namespace IdentityDomain;

public sealed class SSOUserRoot : AggregateRootBase
{
    private readonly IEncryptionService _encryptionService;

    public static Result<SSOUserRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IEncryptionService encryptionService,
        string providerName, Identifier userId)
    {
        var root = new SSOUserRoot(recorder, idFactory, encryptionService);
        root.RaiseCreateEvent(IdentityDomain.Events.SSOUsers.Created(root.Id, providerName, userId));
        return root;
    }

    private SSOUserRoot(IRecorder recorder, IIdentifierFactory idFactory, IEncryptionService encryptionService) : base(
        recorder, idFactory)
    {
        _encryptionService = encryptionService;
    }

    private SSOUserRoot(IRecorder recorder, IIdentifierFactory idFactory, IEncryptionService encryptionService,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _encryptionService = encryptionService;
    }

    public Optional<Address> Address { get; private set; }

    public Optional<EmailAddress> EmailAddress { get; private set; }

    public Optional<PersonName> Name { get; private set; }

    public Optional<string> ProviderName { get; private set; }

    public Optional<Timezone> Timezone { get; private set; }

    public Optional<string> Tokens { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public static AggregateRootFactory<SSOUserRoot> Rehydrate()
    {
        return (identifier, container, _) => new SSOUserRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), container.GetRequiredService<IEncryptionService>(),
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

            case TokensUpdated changed:
            {
                var emailAddress = Domain.Shared.EmailAddress.Create(changed.EmailAddress);
                if (emailAddress.IsFailure)
                {
                    return emailAddress.Error;
                }

                var name = PersonName.Create(changed.FirstName, changed.LastName);
                if (name.IsFailure)
                {
                    return name.Error;
                }

                var timezone = Domain.Shared.Timezone.Create(changed.Timezone);
                if (timezone.IsFailure)
                {
                    return timezone.Error;
                }

                var address = Domain.Shared.Address.Create(CountryCodes.FindOrDefault(changed.CountryCode));
                if (address.IsFailure)
                {
                    return address.Error;
                }

                var tokens = _encryptionService.Decrypt(changed.Tokens);
                Name = name.Value;
                EmailAddress = emailAddress.Value;
                Timezone = timezone.Value;
                Address = address.Value;
                Tokens = tokens;
                Recorder.TraceDebug(null, "User {Id} has updated their tokens", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> UpdateDetails(SSOAuthTokens tokens, EmailAddress emailAddress,
        PersonName name, Timezone timezone, Address address)
    {
        var secureTokens = _encryptionService.Encrypt(tokens.ToJson(false)!);
        return RaiseChangeEvent(
            IdentityDomain.Events.SSOUsers.TokensUpdated(Id, secureTokens, emailAddress, name, timezone,
                address));
    }
}