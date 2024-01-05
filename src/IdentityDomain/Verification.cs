using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace IdentityDomain;

public sealed class Verification : ValueObjectBase<Verification>
{
    public static readonly TimeSpan DefaultTokenExpiry = TimeSpan.FromDays(1);

    public static Result<Verification, Error> Create()
    {
        return new Verification(Optional<string>.None, Optional<DateTime>.None, Optional<DateTime>.None);
    }

    public static Result<Verification, Error> Create(Optional<string> token, Optional<DateTime> expiresUtc,
        Optional<DateTime> verifiedUtc)
    {
        return new Verification(token, expiresUtc, verifiedUtc);
    }

    private Verification(Optional<string> token, Optional<DateTime> expiresUtc, Optional<DateTime> verifiedUtc)
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

    public static ValueObjectFactory<Verification> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new Verification(parts[0].ToOptional(), parts[1].FromIso8601().ToOptional(),
                parts[2].FromIso8601().ToOptional());
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { Token, ExpiresUtc, VerifiedUtc };
    }

#pragma warning disable CA1822
    public Verification Renew(string token)
#pragma warning restore CA1822
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        return new Verification(token, DateTime.UtcNow.Add(DefaultTokenExpiry), Optional<DateTime>.None);
    }

#if TESTINGONLY
    public Verification TestingOnly_ExpireToken()
    {
        return new Verification(Token, DateTime.UtcNow, Optional<DateTime>.None);
    }
#endif

#pragma warning disable CA1822
    public Verification Verify()
#pragma warning restore CA1822
    {
        return new Verification(Optional<string>.None, Optional<DateTime>.None, DateTime.UtcNow.SubtractSeconds(1));
    }
}