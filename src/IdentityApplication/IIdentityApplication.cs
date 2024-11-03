using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace IdentityApplication;

public interface IIdentityApplication
{
    Task<Result<Identity, Error>> GetIdentityAsync(ICallerContext caller, CancellationToken cancellationToken);
}