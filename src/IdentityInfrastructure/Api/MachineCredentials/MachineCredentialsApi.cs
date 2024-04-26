using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MachineCredentials;

public class MachineCredentialsApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly IMachineCredentialsApplication _machineCredentialsApplication;

    public MachineCredentialsApi(ICallerContextFactory callerFactory,
        IMachineCredentialsApplication machineCredentialsApplication)
    {
        _callerFactory = callerFactory;
        _machineCredentialsApplication = machineCredentialsApplication;
    }

    public async Task<ApiPostResult<MachineCredential, RegisterMachineResponse>> RegisterMachine(
        RegisterMachineRequest request, CancellationToken cancellationToken)
    {
        var machine = await _machineCredentialsApplication.RegisterMachineAsync(_callerFactory.Create(), request.Name,
            request.Timezone, request.CountryCode, request.ApiKeyExpiresOnUtc, cancellationToken);

        return () => machine.HandleApplicationResult<MachineCredential, RegisterMachineResponse>(x =>
            new PostResult<RegisterMachineResponse>(new RegisterMachineResponse { Machine = x }));
    }
}