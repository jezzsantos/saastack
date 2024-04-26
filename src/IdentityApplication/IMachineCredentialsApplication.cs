using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IMachineCredentialsApplication
{
    Task<Result<MachineCredential, Error>> RegisterMachineAsync(ICallerContext caller, string name,
        string? timezone, string? countryCode, DateTime? apiKeyExpiresOn, CancellationToken cancellationToken);
}