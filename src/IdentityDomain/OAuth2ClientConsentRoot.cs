using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.OAuth2.ClientConsents;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;
using Created = Domain.Events.Shared.Identities.OAuth2.ClientConsents.Created;

namespace IdentityDomain;

public sealed class OAuth2ClientConsentRoot : AggregateRootBase
{
    public static Result<OAuth2ClientConsentRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier clientId, Identifier userId)
    {
        var root = new OAuth2ClientConsentRoot(recorder, idFactory);
        root.RaiseCreateEvent(IdentityDomain.Events.OAuth2.ClientConsents.Created(root.Id, clientId, userId));
        return root;
    }

    private OAuth2ClientConsentRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private OAuth2ClientConsentRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Identifier ClientId { get; private set; } = Identifier.Empty();

    public bool IsConsented { get; private set; }

    public OAuth2Scopes Scopes { get; private set; } = OAuth2Scopes.Empty;

    public Identifier UserId { get; private set; } = Identifier.Empty();

    [UsedImplicitly]
    public static AggregateRootFactory<OAuth2ClientConsentRoot> Rehydrate()
    {
        return (identifier, container, _) => new OAuth2ClientConsentRoot(
            container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
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
                ClientId = created.ClientId.ToId();
                UserId = created.UserId.ToId();
                IsConsented = created.IsConsented;
                Scopes = OAuth2Scopes.Empty;
                return Result.Ok;
            }

            case ConsentChanged changed:
            {
                IsConsented = changed.IsConsented;
                var scopes = OAuth2Scopes.Create(changed.Scopes);
                if (scopes.IsFailure)
                {
                    return scopes.Error;
                }

                Scopes = scopes.Value;
                Recorder.TraceDebug(null,
                    "OAuth2ClientConsent {Id} has changed its consent to {IsConsented} with scopes {Scopes}", Id,
                    changed.IsConsented, changed.Scopes.JoinAsOredChoices());
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> ChangeConsent(Identifier modifierId, bool isConsented, OAuth2Scopes scopes)
    {
        if (!IsOwner(modifierId))
        {
            return Error.PreconditionViolation(Resources.OAuth2ClientConsentRoot_NotOwner);
        }

        if (!scopes.Has(OpenIdConnectConstants.Scopes.OpenId))
        {
            return Error.Validation(Resources.OAuth2ClientConsentRoot_MissingOpenIdScope,
                OAuth2Constants.ErrorCodes.InvalidScope);
        }

        var nothingHasChanged = isConsented == IsConsented && scopes == Scopes;
        if (nothingHasChanged)
        {
            return Result.Ok;
        }

        return RaiseChangeEvent(IdentityDomain.Events.OAuth2.ClientConsents.ConsentChanged(Id, isConsented, scopes));
    }

    public Result<bool, Error> HasConsentedTo(OAuth2Scopes scopes)
    {
        if (!IsConsented)
        {
            return false;
        }

        return Scopes.HasAll(scopes);
    }

    public Result<bool, Error> Revoke(Identifier revokerId)
    {
        if (!IsOwner(revokerId))
        {
            return Error.PreconditionViolation(Resources.OAuth2ClientConsentRoot_NotOwner);
        }

        if (!IsConsented)
        {
            return false;
        }

        var revoked =
            RaiseChangeEvent(IdentityDomain.Events.OAuth2.ClientConsents.ConsentChanged(Id, false, OAuth2Scopes.Empty));
        if (revoked.IsFailure)
        {
            return revoked.Error;
        }

        return true;
    }

    private bool IsOwner(Identifier userId)
    {
        return userId == UserId;
    }
}