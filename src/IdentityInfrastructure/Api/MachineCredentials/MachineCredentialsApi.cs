using Application.Interfaces;
using Application.Resources.Shared;
using IdentityApplication;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Identities;

namespace IdentityInfrastructure.Api.MachineCredentials;

public class MachineCredentialsApi : IWebApiService
{
    private readonly ICallerContext _context;
    private readonly IMachineCredentialsApplication _machineCredentialsApplication;

    public MachineCredentialsApi(ICallerContext context, IMachineCredentialsApplication machineCredentialsApplication)
    {
        _context = context;
        _machineCredentialsApplication = machineCredentialsApplication;
    }

    public async Task<ApiPostResult<MachineCredential, RegisterMachineResponse>> RegisterMachine(
        RegisterMachineRequest request,
        CancellationToken cancellationToken)
    {
        var machine = await _machineCredentialsApplication.RegisterMachineAsync(_context, request.Name,
            request.Timezone, request.CountryCode, cancellationToken);

        return () => machine.HandleApplicationResult<RegisterMachineResponse, MachineCredential>(x =>
            new PostResult<RegisterMachineResponse>(new RegisterMachineResponse { Machine = x }));
    }
}