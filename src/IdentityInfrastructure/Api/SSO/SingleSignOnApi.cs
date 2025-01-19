using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.SSO;

public class SingleSignOnApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ISingleSignOnApplication _singleSignOnApplication;

    public SingleSignOnApi(ICallerContextFactory callerFactory,
        ISingleSignOnApplication singleSignOnApplication)
    {
        _callerFactory = callerFactory;
        _singleSignOnApplication = singleSignOnApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticateResponse>> Authenticate(
        AuthenticateSingleSignOnRequest request, CancellationToken cancellationToken)
    {
        var authenticated =
            await _singleSignOnApplication.AuthenticateAsync(_callerFactory.Create(), request.InvitationToken,
                request.Provider!, request.AuthCode!, request.Username, request.TermsAndConditionsAccepted,
                cancellationToken);

        return () => authenticated.HandleApplicationResult<AuthenticateTokens, AuthenticateResponse>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse
            {
                Tokens = tok
            }));
    }
}