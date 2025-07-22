using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.Identities;

public class IdentityApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IIdentityApplication _identityApplication;

    public IdentityApi(ICallerContextFactory callerFactory, IIdentityApplication identityApplication)
    {
        _callerFactory = callerFactory;
        _identityApplication = identityApplication;
    }

    public async Task<ApiGetResult<Identity, GetIdentityResponse>> GetIdentity(GetIdentityForCallerRequest request,
        CancellationToken cancellationToken)
    {
        var identity = await _identityApplication.GetIdentityAsync(_callerFactory.Create(), cancellationToken);

        return () =>
            identity.HandleApplicationResult<Identity, GetIdentityResponse>(id => new GetIdentityResponse
                { Identity = id });
    }
}