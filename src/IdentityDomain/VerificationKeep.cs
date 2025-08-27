using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class VerificationKeep : ValueObjectBase<VerificationKeep>
{
    public static readonly TimeSpan DefaultTokenExpiry = TimeSpan.FromDays(1);

    public static Result<VerificationKeep, Error> Create()
    {
        return new VerificationKeep(Optional<string>.None, Optional<DateTime>.None, Optional<DateTime>.None);
    }

    public static Result<VerificationKeep, Error> Create(Optional<string> token, Optional<DateTime> expiresUtc,
        Optional<DateTime> verifiedUtc)
    {
        if (token.HasValue)
        {
            if (token.Value.IsInvalidParameter(Validations.Credentials.Password.VerificationToken, nameof(token),
                    Resources.VerificationKeep_InvalidToken, out var error1))
            {
                return error1;
            }
        }

        return new VerificationKeep(token, expiresUtc, verifiedUtc);
    }

    private VerificationKeep(Optional<string> token, Optional<DateTime> expiresUtc, Optional<DateTime> verifiedUtc)
    {
        Token = token;
        ExpiresUtc = expiresUtc;
        VerifiedUtc = verifiedUtc;
    }

    public Optional<DateTime> ExpiresUtc { get; }

    public bool IsStillVerifying => IsVerifying && ExpiresUtc > DateTime.UtcNow;

    public bool IsVerifiable => !Token.HasValue && !ExpiresUtc.HasValue && !VerifiedUtc.HasValue;

    public bool IsVerified => !Token.HasValue && !ExpiresUtc.HasValue && VerifiedUtc.HasValue;

    public bool IsVerifying => Token.HasValue && ExpiresUtc.HasValue;

    public Optional<string> Token { get; }

    public Optional<DateTime> VerifiedUtc { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<VerificationKeep> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new VerificationKeep(
                parts[0],
                parts[1].ToOptional(val => val.FromIso8601()),
                parts[2].ToOptional(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Token, ExpiresUtc, VerifiedUtc];
    }

#pragma warning disable CA1822
    public VerificationKeep Renew(string token)
#pragma warning restore CA1822
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        return new VerificationKeep(token, DateTime.UtcNow.Add(DefaultTokenExpiry), Optional<DateTime>.None);
    }

#if TESTINGONLY
    public VerificationKeep TestingOnly_ExpireToken()
    {
        return new VerificationKeep(Token, DateTime.UtcNow, Optional<DateTime>.None);
    }
#endif

#pragma warning disable CA1822
    public VerificationKeep Verify()
#pragma warning restore CA1822
    {
        return new VerificationKeep(Optional<string>.None, Optional<DateTime>.None, DateTime.UtcNow.SubtractSeconds(1));
    }
}