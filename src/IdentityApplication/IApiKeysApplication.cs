using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IApiKeysApplication
{
    Task<Result<MachineCredential, Error>> RegisterMachineAsync(ICallerContext context, string name, string? timezone,
        string? countryCode, CancellationToken cancellationToken);
}