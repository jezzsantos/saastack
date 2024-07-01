using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Signings;
using SigningsApplication;

namespace SigningsInfrastructure.Api.Signings;

public class SigningApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ISigningsApplication _signingsApplication;

    public SigningApi(ICallerContextFactory callerFactory, ISigningsApplication signingsApplication)
    {
        _callerFactory = callerFactory;
        _signingsApplication = signingsApplication;
    }

    public async Task<ApiPostResult<SigningRequest, CreateDraftSigningRequestResponse>> CreateDraft(
        CreateDraftSigningRequestRequest request, CancellationToken cancellationToken)
    {
        var signingRequest = await _signingsApplication.CreateDraftAsync(_callerFactory.Create(),
            request.OrganizationId!, request.Signees, cancellationToken);

        return () =>
            signingRequest.HandleApplicationResult<SigningRequest, CreateDraftSigningRequestResponse>(sr =>
                new PostResult<CreateDraftSigningRequestResponse>(
                    new CreateDraftSigningRequestResponse
                    {
                        SigningRequest = sr
                    }));
    }
}