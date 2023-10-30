using Common;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a Application Layer repository port
/// </summary>
public interface IApplicationRepository
{
    Task<Result<Error>> DestroyAllAsync();
}