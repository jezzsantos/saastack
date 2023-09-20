using Application.Interfaces;
using Application.Interfaces.Resources;

namespace CarsApplication;

public interface ICarsApplication
{
    Task<Car> GetCarAsync(ICallerContext caller, string id, CancellationToken cancellationToken);
}