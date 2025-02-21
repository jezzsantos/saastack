using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using IdentityDomain.DomainServices;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class APIKeyKeep : ValueObjectBase<APIKeyKeep>
{
    public static Result<APIKeyKeep, Error> Create(IAPIKeyHasherService apiKeyHasherService, string token,
        string keyHash)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error1))
        {
            return error1;
        }

        if (keyHash.IsNotValuedParameter(nameof(keyHash), out var error2))
        {
            return error2;
        }

        if (keyHash.IsInvalidParameter(apiKeyHasherService.ValidateAPIKeyHash, nameof(keyHash),
                Resources.ApiKeyKeep_InvalidKeyHash, out var error3))
        {
            return error3;
        }

        return new APIKeyKeep(token, keyHash);
    }

    private APIKeyKeep(string token, string keyHash)
    {
        KeyHash = keyHash;
        Token = token;
    }

    public string KeyHash { get; private set; }

    public string Token { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<APIKeyKeep> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new APIKeyKeep(parts[0]!, parts[1]!);
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { Token, KeyHash };
    }

    public Result<APIKeyKeep, Error> ChangeKey(IAPIKeyHasherService apiKeyHasherService, string token, string keyHash)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error1))
        {
            return error1;
        }

        if (keyHash.IsNotValuedParameter(nameof(keyHash), out var error2))
        {
            return error2;
        }

        if (keyHash.IsInvalidParameter(apiKeyHasherService.ValidateAPIKeyHash, nameof(keyHash),
                Resources.ApiKeyKeep_InvalidKeyHash, out var error3))
        {
            return error3;
        }

        return new APIKeyKeep(token, keyHash);
    }

#if TESTINGONLY
    [SkipImmutabilityCheck]
    public void TestingOnly_ResetKeyHash()
    {
        KeyHash = string.Empty;
    }
#endif

    [SkipImmutabilityCheck]
    public Result<bool, Error> Verify(IAPIKeyHasherService apiKeyHasherService, string key)
    {
        if (key.IsNotValuedParameter(nameof(key), out var error1))
        {
            return error1;
        }

        if (key.IsInvalidParameter(apiKeyHasherService.ValidateKey, nameof(key),
                Resources.ApiKeyKeep_InvalidKey, out var error2))
        {
            return error2;
        }

        if (KeyHash.HasNoValue())
        {
            return Error.RuleViolation(Resources.ApiKeyKeep_NoApiKeyHash);
        }

        return apiKeyHasherService.VerifyAPIKey(key, KeyHash);
    }
}