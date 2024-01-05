using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using IdentityDomain.DomainServices;

namespace IdentityDomain;

public sealed class PasswordKeep : ValueObjectBase<PasswordKeep>
{
    public static readonly TimeSpan DefaultResetExpiry = TimeSpan.FromHours(2);

    public static Result<PasswordKeep, Error> Create()
    {
        return new PasswordKeep(Optional<string>.None, Optional<string>.None, Optional<DateTime>.None);
    }

    public static Result<PasswordKeep, Error> Create(IPasswordHasherService passwordHasherService, string passwordHash)
    {
        if (passwordHash.IsInvalidParameter(passwordHasherService.ValidatePasswordHash, nameof(passwordHash),
                Resources.PasswordKeep_InvalidPasswordHash, out var error1))
        {
            return error1;
        }

        return new PasswordKeep(passwordHash, Optional<string>.None, Optional<DateTime>.None);
    }

    private PasswordKeep(Optional<string> passwordHash, Optional<string> token,
        Optional<DateTime> tokenExpiresUtc)
    {
        PasswordHash = passwordHash;
        Token = token;
        TokenExpiresUtc = tokenExpiresUtc;
    }

    public bool IsInitiated => PasswordHash.HasValue;

    public bool IsInitiating => Token.HasValue && TokenExpiresUtc.HasValue;

    public bool IsInitiatingStillValid => IsInitiating && TokenExpiresUtc > DateTime.UtcNow;

    public bool IsReset => !Token.HasValue && !TokenExpiresUtc.HasValue;

    public Optional<string> PasswordHash { get; }

    public Optional<string> Token { get; }

    public Optional<DateTime> TokenExpiresUtc { get; }

    public static ValueObjectFactory<PasswordKeep> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new PasswordKeep(parts[0].ToOptional(), parts[1].ToOptional(),
                parts[2].FromIso8601().ToOptional());
        };
    }

    protected override IEnumerable<object> GetAtomicValues()
    {
        return new object[] { PasswordHash, Token, TokenExpiresUtc };
    }

    public Result<PasswordKeep, Error> ConfirmReset(string token)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error1))
        {
            return error1;
        }

        if (token.IsInvalidParameter(Validations.Credentials.Password.ResetToken, nameof(token),
                Resources.PasswordKeep_InvalidToken, out var error2))
        {
            return error2;
        }

        if (token.NotEqualsOrdinal(Token))
        {
            return Error.RuleViolation(Resources.PasswordKeep_TokensNotMatch);
        }

        if (!IsInitiatingStillValid)
        {
            return Error.PreconditionViolation(Resources.PasswordKeep_TokenExpired);
        }

        return this;
    }

    public Result<PasswordKeep, Error> InitiatePasswordReset(string token)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error1))
        {
            return error1;
        }

        if (token.IsInvalidParameter(Validations.Credentials.Password.ResetToken, nameof(token),
                Resources.PasswordKeep_InvalidToken, out var error2))
        {
            return error2;
        }

        if (!IsInitiated)
        {
            return Error.RuleViolation(Resources.PasswordKeep_NoPasswordHash);
        }

        var expiry = DateTime.UtcNow.Add(DefaultResetExpiry);
        return new PasswordKeep(PasswordHash, token, expiry);
    }

    public Result<PasswordKeep, Error> ResetPassword(IPasswordHasherService passwordHasherService, string token,
        string passwordHash)
    {
        if (token.IsNotValuedParameter(nameof(token), out var error1))
        {
            return error1;
        }

        if (token.IsInvalidParameter(Validations.Credentials.Password.ResetToken, nameof(token),
                Resources.PasswordKeep_InvalidToken, out var error2))
        {
            return error2;
        }

        if (passwordHash.IsNotValuedParameter(nameof(passwordHash), out var error3))
        {
            return error3;
        }

        if (passwordHash.IsInvalidParameter(passwordHasherService.ValidatePasswordHash,
                nameof(passwordHash), Resources.PasswordKeep_InvalidPasswordHash, out var error4))
        {
            return error4;
        }

        if (!IsInitiated)
        {
            return Error.RuleViolation(Resources.PasswordKeep_NoPasswordHash);
        }

        if (token.NotEqualsOrdinal(Token))
        {
            return Error.RuleViolation(Resources.PasswordKeep_TokensNotMatch);
        }

        if (!IsInitiatingStillValid)
        {
            return Error.PreconditionViolation(Resources.PasswordKeep_TokenExpired);
        }

        return new PasswordKeep(passwordHash, Optional<string>.None, Optional<DateTime>.None);
    }

    public Result<PasswordKeep, Error> SetPassword(IPasswordHasherService passwordHasherService, string passwordHash)
    {
        if (passwordHash.IsNotValuedParameter(nameof(passwordHash), out var error1))
        {
            return error1;
        }

        if (passwordHash.IsInvalidParameter(passwordHasherService.ValidatePasswordHash,
                nameof(passwordHash), Resources.PasswordKeep_InvalidPasswordHash, out var error2))
        {
            return error2;
        }

        return new PasswordKeep(passwordHash, Optional<string>.None, Optional<DateTime>.None);
    }

#if TESTINGONLY

    public PasswordKeep TestingOnly_ExpireToken()
    {
        return new PasswordKeep(PasswordHash, Token, DateTime.UtcNow.SubtractSeconds(1));
    }
#endif

    [SkipImmutabilityCheck]
    public Result<bool, Error> Verify(IPasswordHasherService passwordHasherService, string password)
    {
        if (password.IsNotValuedParameter(nameof(password), out var error1))
        {
            return error1;
        }

        if (password.IsInvalidParameter(pwd => passwordHasherService.ValidatePassword(pwd, false),
                nameof(password), Resources.PasswordKeep_InvalidPassword, out var error2))
        {
            return error2;
        }

        if (!IsInitiated)
        {
            return Error.RuleViolation(Resources.PasswordKeep_NoPasswordHash);
        }

        return passwordHasherService.VerifyPassword(password, PasswordHash);
    }
}