using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace SigningsApplication;

public interface ISigningsApplication
{
    Task<Result<SigningRequest, Error>> CreateDraftAsync(ICallerContext caller, string organizationId,
        List<Signee> signees, CancellationToken cancellationToken);
}