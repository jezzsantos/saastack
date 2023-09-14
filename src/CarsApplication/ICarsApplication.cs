using Application.Interfaces;

namespace CarsApplication;

public interface ICarsApplication
{
    Task<string> GetCarAsync(ICallerContext caller, string? id, CancellationToken cancellationToken);
}