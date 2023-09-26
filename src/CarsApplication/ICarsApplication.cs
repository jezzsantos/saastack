using Application.Interfaces;
using Application.Interfaces.Resources;
using Common;

namespace CarsApplication;

public interface ICarsApplication
{
    Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string id, CancellationToken cancellationToken);
}