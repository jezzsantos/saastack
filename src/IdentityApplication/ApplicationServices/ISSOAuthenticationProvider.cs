using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines a Single Sign On provider of authentication services
/// </summary>
public interface ISSOAuthenticationProvider
{
    string ProviderName { get; }

    /// <summary>
    ///     Returns the authenticated user with the specified <see cref="authCode" /> for the specified
    ///     <see cref="emailAddress" />
    /// </summary>
    Task<Result<SSOAuthUserInfo, Error>> AuthenticateAsync(ICallerContext caller, string authCode, string? codeVerifier,
        string? emailAddress,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the refreshed token, with new access tokens
    /// </summary>
    Task<Result<ProviderAuthenticationTokens, Error>> RefreshTokenAsync(ICallerContext caller, string refreshToken,
        CancellationToken cancellationToken);
}

/// <summary>
///     Provides the information about user info from a 3rd party system
/// </summary>
public class SSOAuthUserInfo
{
    public SSOAuthUserInfo(IReadOnlyList<AuthToken> tokens, string uId, string emailAddress, string firstName,
        string? lastName, TimezoneIANA timezone, Bcp47Locale locale, CountryCodeIso3166 countryCode)
    {
        Tokens = tokens;
        UId = uId;
        EmailAddress = emailAddress;
        FirstName = firstName;
        LastName = lastName;
        Timezone = timezone;
        Locale = locale;
        CountryCode = countryCode;
    }

    public CountryCodeIso3166 CountryCode { get; }

    public string EmailAddress { get; }

    public string FirstName { get; }

    public string FullName => LastName.HasValue()
        ? $"{FirstName} {LastName}"
        : FirstName;

    public string? LastName { get; }

    public Bcp47Locale Locale { get; }

    public TimezoneIANA Timezone { get; }

    public IReadOnlyList<AuthToken> Tokens { get; }

    public string UId { get; }
}