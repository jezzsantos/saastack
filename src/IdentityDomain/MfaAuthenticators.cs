using System.Collections;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Identities;

namespace IdentityDomain;

public class MfaAuthenticators : IReadOnlyList<MfaAuthenticator>
{
    private const int MaxGeneratedRecoveryCodes = 16;
    private const string RecoveryCodeDelimiter = ";";
    private readonly List<MfaAuthenticator> _authenticators = new();

    public bool HasAnyConfirmedPlusRecoveryCodes =>
        _authenticators.Count > 1
        && _authenticators.Any(auth => auth.Type != MfaAuthenticatorType.RecoveryCodes && auth.HasBeenConfirmed);

    public bool HasOnlyOneUnconfirmedPlusRecoveryCodes =>
        _authenticators.Count == 2
        && _authenticators[0].Type == MfaAuthenticatorType.RecoveryCodes;

    public bool HasOnlyRecoveryCodes =>
        _authenticators.Count == 1
        && _authenticators[0].Type == MfaAuthenticatorType.RecoveryCodes;

    public Result<Error> EnsureInvariants()
    {
        _authenticators
            .ForEach(una => una.EnsureInvariants());

        return Result.Ok;
    }

    public int Count => _authenticators.Count;

    public IEnumerator<MfaAuthenticator> GetEnumerator()
    {
        return _authenticators.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public MfaAuthenticator this[int index] => _authenticators[index];

    public void Add(MfaAuthenticator authenticator)
    {
        var match = FindByType(authenticator.Type);
        if (match.Exists())
        {
            _authenticators.Remove(match);
        }

        _authenticators.Add(authenticator);
    }

    public Optional<MfaAuthenticator> FindById(Identifier authenticatorId)
    {
        var authenticator = _authenticators.FirstOrDefault(auth => auth.Id == authenticatorId);
        return authenticator.NotExists()
            ? Optional<MfaAuthenticator>.None
            : authenticator;
    }

    public Optional<MfaAuthenticator> FindByType(MfaAuthenticatorType type)
    {
        var authenticator = _authenticators.FirstOrDefault(auth => auth.Type == type);
        return authenticator.NotExists()
            ? Optional<MfaAuthenticator>.None
            : authenticator;
    }

    public Optional<MfaAuthenticator> FindRecoveryCodes()
    {
        var recoveryCodes = _authenticators.FirstOrDefault(auth => auth.Type == MfaAuthenticatorType.RecoveryCodes);
        return recoveryCodes.NotExists()
            ? Optional<MfaAuthenticator>.None
            : recoveryCodes;
    }

    public static string GenerateRecoveryCodes()
    {
        var codes = new List<string>();
        Repeat.Times(() =>
        {
            var code = Guid.NewGuid().ToString("D").Substring(0, 8);
            codes.Add(code);
        }, MaxGeneratedRecoveryCodes);

        return codes.Join(RecoveryCodeDelimiter);
    }

    public static List<string> ParseRecoveryCodes(IEncryptionService encryptionService, string encryptedCodes)
    {
        var decryptedCodes = encryptionService.Decrypt(encryptedCodes);
        if (decryptedCodes.HasNoValue())
        {
            return [];
        }

        return decryptedCodes
            .Split(RecoveryCodeDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public static List<string> ParseRecoveryCodes(string recoveryCodes)
    {
        if (recoveryCodes.HasNoValue())
        {
            return [];
        }

        return recoveryCodes
            .Split(RecoveryCodeDelimiter, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    public void Remove(Identifier authenticatorId)
    {
        var unavailability = _authenticators.Find(auth => auth.Id == authenticatorId);
        if (unavailability.Exists())
        {
            _authenticators.Remove(unavailability);
        }
    }

    public List<string> ToRecoveryCodes(IEncryptionService encryptionService)
    {
        var recoveryCodesAuthenticator = FindRecoveryCodes();
        if (recoveryCodesAuthenticator.NotExists())
        {
            return new List<string>();
        }

        var secret = recoveryCodesAuthenticator.Value.Secret.ValueOrDefault!;
        return ParseRecoveryCodes(encryptionService, secret);
    }

    public IReadOnlyList<MfaAuthenticator> WithoutRecoveryCodes()
    {
        return _authenticators
            .Where(auth => auth.Type != MfaAuthenticatorType.RecoveryCodes)
            .ToList();
    }
}