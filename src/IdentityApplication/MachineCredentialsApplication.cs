using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public class MachineCredentialsApplication : IMachineCredentialsApplication
{
    public async Task<Result<MachineCredential, Error>> RegisterMachineAsync(ICallerContext context, string name,
        string? timezone, string? countryCode,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}