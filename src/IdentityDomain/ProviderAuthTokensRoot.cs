using Common;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.ProviderAuthTokens;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;
using QueryAny;

namespace IdentityDomain;

/// <summary>
///     Manages the authentication tokens that are harvested from external 3rd party providers.
///     Note: It is updated every time the external provider authenticates the user, and provides new tokens.
///     Tokens change all the time, we can assume at worst, every 15 minutes, for every day of use for every user.
///     This is why this aggregate is using snapshotting and not event-sourcing.
///     We need to store the <see cref="Tokens" /> value encrypted at rest.
/// </summary>
[EntityName("ProviderAuthTokens")]
public sealed class ProviderAuthTokensRoot : AggregateRootBase
{
    public static Result<ProviderAuthTokensRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        string providerName, Identifier userId)
    {
        var root = new ProviderAuthTokensRoot(recorder, idFactory);
        root.RaiseCreateEvent(IdentityDomain.Events.ProviderAuthTokens.Created(root.Id, userId, providerName));
        return root;
    }

    private ProviderAuthTokensRoot(ISingleValueObject<string> identifier, IDependencyContainer container,
        HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
    {
        ProviderName = rehydratingProperties.GetValueOrDefault<string>(nameof(ProviderName));
        UserId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(UserId));
        Tokens = rehydratingProperties.GetValueOrDefault<Optional<AuthTokens>>(nameof(Tokens));
    }

    private ProviderAuthTokensRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    public string ProviderName { get; private set; } = string.Empty;

    public Optional<AuthTokens> Tokens { get; private set; }

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public override HydrationProperties Dehydrate()
    {
        var properties = base.Dehydrate();
        properties.Add(nameof(ProviderName), ProviderName);
        properties.Add(nameof(UserId), UserId);
        properties.Add(nameof(Tokens), Tokens);
        return properties;
    }

    [UsedImplicitly]
    public static AggregateRootFactory<ProviderAuthTokensRoot> Rehydrate()
    {
        return (identifier, container, properties) => new ProviderAuthTokensRoot(identifier, container, properties);
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
                var tokens = AuthTokens.Create(changed.Tokens);
                if (tokens.IsFailure)
                {
                    return tokens.Error;
                }

                Tokens = tokens.Value;
                Recorder.TraceDebug(null, "User {UserId} has changed their tokens for provider {ProviderName}", UserId,
                    ProviderName);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> ChangeTokens(Identifier modifierId, AuthTokens tokens)
    {
        if (Tokens.HasValue && !IsOwner(modifierId))
        {
            return Error.RoleViolation(Resources.ProviderAuthTokensRoot_NotOwner);
        }

        return RaiseChangeEvent(IdentityDomain.Events.ProviderAuthTokens.TokensChanged(Id, tokens));
    }

    private bool IsOwner(Identifier userId)
    {
        return userId == UserId;
    }
}