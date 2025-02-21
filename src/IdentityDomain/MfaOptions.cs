using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class MfaOptions : ValueObjectBase<MfaOptions>
{
    public static readonly MfaOptions Default = new(false, true, Optional<string>.None, Optional<DateTime>.None);
    internal static readonly TimeSpan DefaultAuthenticationTokenExpiry = TimeSpan.FromMinutes(10);

    public static Result<MfaOptions, Error> Create(bool isEnabled, bool canBeDisabled,
        string authenticationToken, DateTime authenticationExpiresAt)
    {
        if (authenticationToken.HasValue() && !authenticationExpiresAt.HasValue())
        {
            return Error.Validation(Resources.MfaOptions_TokenWithoutExpiry);
        }

        return new MfaOptions(isEnabled, canBeDisabled, authenticationToken, authenticationExpiresAt);
    }

    public static Result<MfaOptions, Error> Create(bool isEnabled, bool canBeDisabled)
    {
        return Create(isEnabled, canBeDisabled, Optional<string>.None, Optional<DateTime>.None);
    }

    private MfaOptions(bool isEnabled, bool canBeDisabled,
        Optional<string> authenticationToken, Optional<DateTime> tokenExpiresAtUtc)
    {
        IsEnabled = isEnabled;
        CanBeDisabled = canBeDisabled;
        AuthenticationToken = authenticationToken;
        AuthenticationTokenExpiresAt = tokenExpiresAtUtc;
    }

    public Optional<string> AuthenticationToken { get; }

    public Optional<DateTime> AuthenticationTokenExpiresAt { get; private set; }

    public bool CanBeDisabled { get; }

    public bool IsAuthenticationExpired =>
        IsAuthenticationInitiated && AuthenticationTokenExpiresAt < DateTime.UtcNow;

    public bool IsAuthenticationInitiated => AuthenticationToken.HasValue;

    public bool IsEnabled { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<MfaOptions> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new MfaOptions(parts[0]!.ToBoolOrDefault(false),
                parts[1]!.ToBoolOrDefault(true),
                parts[2].FromValueOrNone<string, string>(),
                parts[3].FromValueOrNone(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[]
            { IsEnabled, CanBeDisabled, AuthenticationToken, AuthenticationTokenExpiresAt };
    }

    [SkipImmutabilityCheck]
    public Result<Error> Authenticate(MfaCaller caller)
    {
        if (!IsEnabled)
        {
            return Error.RuleViolation(Resources.MfaOptions_NotEnabled);
        }

        if (caller.IsAuthenticated)
        {
            return Result.Ok;
        }

        if (!IsAuthenticationInitiated)
        {
            return Error.RuleViolation(Resources.MfaOptions_AuthenticationNotInitiated);
        }

        if (caller.AuthenticationToken != AuthenticationToken)
        {
            return Error.NotAuthenticated(Resources.MfaOptions_AuthenticationFailed);
        }

        if (IsAuthenticationExpired)
        {
            return Error.NotAuthenticated(Resources.MfaOptions_AuthenticationTokenExpired);
        }

        return Result.Ok;
    }

    public Result<MfaOptions, Error> Enable(bool isEnabled)
    {
        if (isEnabled == false
            && CanBeDisabled == false)
        {
            return Error.RuleViolation(IsEnabled
                ? Resources.MfaOptions_Change_CannotBeDisabled
                : Resources.MfaOptions_Change_CannotBeEnabled);
        }

        return Create(isEnabled, CanBeDisabled, AuthenticationToken,
            AuthenticationTokenExpiresAt);
    }

    public Result<MfaOptions, Error> InitiateAuthentication(ITokensService tokensService)
    {
        if (!IsEnabled)
        {
            return Error.RuleViolation(Resources.MfaOptions_NotEnabled);
        }

        var authenticationToken = tokensService.CreateMfaAuthenticationToken();
        return Create(IsEnabled, CanBeDisabled, authenticationToken,
            DateTime.UtcNow.Add(DefaultAuthenticationTokenExpiry));
    }

#if TESTINGONLY
    [SkipImmutabilityCheck]
    public void TestingOnly_ExpireAuthentication()
    {
        AuthenticationTokenExpiresAt = DateTime.UtcNow.SubtractSeconds(1);
    }
#endif
}