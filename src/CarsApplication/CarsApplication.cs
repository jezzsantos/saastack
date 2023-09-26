using Application.Interfaces;
using Application.Interfaces.Resources;
using Common;

namespace CarsApplication;

public class CarsApplication : ICarsApplication
{
    public async Task<Result<Car, Error>> GetCarAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return new Car
        {
            Id = id
        };
    }
}