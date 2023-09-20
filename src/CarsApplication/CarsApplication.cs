using Application.Interfaces;
using Application.Interfaces.Resources;

namespace CarsApplication;

public class CarsApplication : ICarsApplication
{
    public async Task<Car> GetCarAsync(ICallerContext caller, string id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return new Car
        {
            Id = id
        };
    }
}