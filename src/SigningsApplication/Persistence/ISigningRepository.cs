using Application.Persistence.Interfaces;
using Common;
using SigningsDomain;

namespace SigningsApplication.Persistence;

public interface ISigningRepository : IApplicationRepository
{
    Task<Result<SigningRequestRoot, Error>> SaveAsync(SigningRequestRoot request, CancellationToken cancellationToken);
}