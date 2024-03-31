using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.SSO;

public class SingleSignOnApi : IWebApiService
{
    private readonly ICallerContextFactory _contextFactory;
    private readonly ISingleSignOnApplication _singleSignOnApplication;

    public SingleSignOnApi(ICallerContextFactory contextFactory,
        ISingleSignOnApplication singleSignOnApplication)
    {
        _contextFactory = contextFactory;
        _singleSignOnApplication = singleSignOnApplication;
    }

    public async Task<ApiPostResult<AuthenticateTokens, AuthenticateResponse>> Authenticate(
        AuthenticateSingleSignOnRequest request, CancellationToken cancellationToken)
    {
        var authenticated =
            await _singleSignOnApplication.AuthenticateAsync(_contextFactory.Create(), request.InvitationToken,
                request.Provider,
                request.AuthCode, request.Username, cancellationToken);

        return () => authenticated.HandleApplicationResult<AuthenticateResponse, AuthenticateTokens>(tok =>
            new PostResult<AuthenticateResponse>(new AuthenticateResponse
            {
                Tokens = tok
            }));
    }
}