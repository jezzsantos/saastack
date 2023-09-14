using Application.Interfaces;

namespace CarsApplication;

public class CarsApplication : ICarsApplication
{
    public async Task<string> GetCarAsync(ICallerContext caller, string? id, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return $"Hello car {id}!";
    }
}