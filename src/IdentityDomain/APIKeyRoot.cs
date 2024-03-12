using Common;
using Common.Extensions;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using Domain.Shared;
using IdentityDomain.DomainServices;

namespace IdentityDomain;

public sealed class APIKeyRoot : AggregateRootBase
{
    private readonly IAPIKeyHasherService _apiKeyHasherService;

    public static Result<APIKeyRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        IAPIKeyHasherService apiKeyHasherService, Identifier userId, APIKeyToken keyToken)
    {
        var root = new APIKeyRoot(recorder, idFactory, apiKeyHasherService);
        root.RaiseCreateEvent(IdentityDomain.Events.APIKeys.Created.Create(root.Id, userId, keyToken.Token,
            apiKeyHasherService.HashAPIKey(keyToken.Key)));
        return root;
    }

    private APIKeyRoot(IRecorder recorder, IIdentifierFactory idFactory, IAPIKeyHasherService apiKeyHasherService) :
        base(recorder, idFactory)
    {
        _apiKeyHasherService = apiKeyHasherService;
    }

    private APIKeyRoot(IRecorder recorder, IIdentifierFactory idFactory, IAPIKeyHasherService apiKeyHasherService,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
        _apiKeyHasherService = apiKeyHasherService;
    }

    public Optional<APIKeyKeep> ApiKey { get; private set; }

    public Optional<string> Description { get; private set; }

    public Optional<DateTime?> ExpiresOn { get; private set; }

    public bool IsKeyExpired => ExpiresOn.HasValue && ExpiresOn < DateTime.UtcNow;

    public Identifier UserId { get; private set; } = Identifier.Empty();

    public static AggregateRootFactory<APIKeyRoot> Rehydrate()
    {
        return (identifier, container, _) => new APIKeyRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), container.GetRequiredService<IAPIKeyHasherService>(),
            identifier);
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        return Result.Ok;
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Events.APIKeys.Created created:
            {
                UserId = created.UserId.ToId();

                var apiKey = APIKeyKeep.Create(_apiKeyHasherService, created.KeyToken, created.KeyHash);
                if (!apiKey.IsSuccessful)
                {
                    return apiKey.Error;
                }

                ApiKey = apiKey.Value;
                return Result.Ok;
            }

            case Events.APIKeys.ParametersChanged changed:
            {
                Description = changed.Description;
                ExpiresOn = changed.ExpiresOn;
                Recorder.TraceDebug(null, "ApiKey {Id} set its parameters", Id);
                return Result.Ok;
            }

            case Events.APIKeys.KeyVerified _:
            {
                Recorder.TraceDebug(null, "ApiKey {Id} verified", Id);
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public Result<Error> Delete(Identifier deleterId)
    {
        if (UserId != deleterId)
        {
            return Error.RuleViolation(Resources.ApiKeyRoot_NotOwner);
        }

        return RaisePermanentDeleteEvent(deleterId);
    }

    public Result<Error> SetParameters(string description, DateTime expiresOn)
    {
        if (description.IsInvalidParameter(Validations.ApiKey.Description, nameof(description),
                Resources.ApiKeyKeep_InvalidDescription, out var error1))
        {
            return error1;
        }

        var lowerLimit = DateTime.UtcNow.ToNearestMinute().Add(Validations.ApiKey.MinimumExpiryPeriod);
        if (expiresOn.IsInvalidParameter(
                exp => exp == lowerLimit || exp.IsAfter(lowerLimit),
                nameof(expiresOn), Resources.APIKeyRoot_ExpiresOnTooSoon, out var error2))
        {
            return error2;
        }

        if (expiresOn.IsInvalidParameter(
                exp => exp.IsBefore(DateTime.UtcNow.ToNearestMinute().Add(Validations.ApiKey.MaximumExpiryPeriod)),
                nameof(expiresOn), Resources.APIKeyRoot_ExpiresOnTooLate, out var error3))
        {
            return error3;
        }

        return RaiseChangeEvent(IdentityDomain.Events.APIKeys.ParametersChanged.Create(Id, description, expiresOn));
    }

    public Result<bool, Error> VerifyKey(string key)
    {
        if (key.IsInvalidParameter(k => _apiKeyHasherService.ValidateKey(k), nameof(key),
                Resources.ApiKeyKeep_InvalidKey, out var error1))
        {
            return error1;
        }

        if (!ApiKey.HasValue)
        {
            return Error.RuleViolation(Resources.APIKeyRoot_Verify_NoApiKey);
        }

        if (IsKeyExpired)
        {
            return false;
        }

        var verified = ApiKey.Value.Verify(_apiKeyHasherService, key);
        if (!verified.IsSuccessful)
        {
            return verified.Error;
        }

        var isVerified = verified.Value;
        var raised = RaiseChangeEvent(IdentityDomain.Events.APIKeys.KeyVerified.Create(Id, isVerified));
        if (!raised.IsSuccessful)
        {
            return raised.Error;
        }

        return verified;
    }
}