#if TESTINGONLY
using Application.Interfaces;
using Common;
using Common.Extensions;
using IdentityApplication.ApplicationServices;
using Infrastructure.Shared;

namespace IdentityInfrastructure.ApplicationServices;

/// <summary>
///     Provides a fake example <see cref="ISSOAuthenticationProvider" /> that can be copied for real providers,
///     such as Google, Microsoft, Facebook and others
/// </summary>
public class FakeSSOAuthenticationProvider : ISSOAuthenticationProvider
{
    public const string SSOName = "testsso";
    private const string ServiceName = "FakeOAuth2Service";
    private readonly IOAuth2Service _auth2Service;

    public FakeSSOAuthenticationProvider() : this(new FakeOAuth2Service())
    {
    }

    private FakeSSOAuthenticationProvider(IOAuth2Service auth2Service)
    {
        _auth2Service = auth2Service;
    }

    public async Task<Result<SSOUserInfo, Error>> AuthenticateAsync(ICallerContext context, string authCode,
        string? emailAddress, CancellationToken cancellationToken)
    {
        if (emailAddress.HasNoValue())
        {
            return Error.RuleViolation(Resources.TestSSOAuthenticationProvider_MissingUsername);
        }

        var retrievedTokens =
            await _auth2Service.ExchangeCodeForTokensAsync(context,
                new OAuth2CodeTokenExchangeOptions(ServiceName, authCode, emailAddress),
                cancellationToken);
        if (!retrievedTokens.IsSuccessful)
        {
            return Error.NotAuthenticated();
        }

        var tokens = retrievedTokens.Value;

        return FakeOAuth2Service.GetInfoFromToken(tokens);
    }

    public string ProviderName => SSOName;
}
#endif