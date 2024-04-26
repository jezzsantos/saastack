using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication.ApplicationServices;

/// <summary>
///     Defines a Single Sign On provider of authentication services
/// </summary>
public interface ISSOAuthenticationProvider
{
    string ProviderName { get; }

    Task<Result<SSOUserInfo, Error>> AuthenticateAsync(ICallerContext caller, string authCode, string? emailAddress,
        CancellationToken cancellationToken);
}

/// <summary>
///     Provides the information about a user from a 3rd party system
/// </summary>
public class SSOUserInfo
{
    public SSOUserInfo(IReadOnlyList<AuthToken> tokens, string emailAddress, string firstName, string? lastName,
        TimezoneIANA timezone, CountryCodeIso3166 countryCode)
    {
        Tokens = tokens;
        EmailAddress = emailAddress;
        FirstName = firstName;
        LastName = lastName;
        Timezone = timezone;
        CountryCode = countryCode;
    }

    public CountryCodeIso3166 CountryCode { get; }

    public string EmailAddress { get; }

    public string FirstName { get; }

    public string? LastName { get; }

    public TimezoneIANA Timezone { get; }

    public IReadOnlyList<AuthToken> Tokens { get; }
}