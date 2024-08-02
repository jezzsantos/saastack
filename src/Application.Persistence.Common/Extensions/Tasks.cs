using Common;

namespace Application.Persistence.Common.Extensions;

public static class Tasks
{
    /// <summary>
    ///     Runs all the specified <see cref="tasks" /> and returns the first <see cref="Error" /> if any
    /// </summary>
    public static async Task<Result<bool, Error>> WhenAllAsync(params Task<Result<bool, Error>>[] tasks)
    {
        var results = await Task.WhenAll(tasks);

        var hasError = results.Any(result => result.IsFailure);
        if (hasError)
        {
            return results.First(result => result.IsFailure).Error;
        }

        return results.All(result => result.Value);
    }

    /// <summary>
    ///     Runs all the specified <see cref="tasks" /> and returns the first <see cref="Error" /> if any
    /// </summary>
    public static async Task<Result<List<TResult>, Error>> WhenAllAsync<TResult>(
        IEnumerable<Task<Result<TResult, Error>>> tasks)
    {
        var results = await Task.WhenAll(tasks);

        var hasError = results.Any(result => result.IsFailure);
        if (hasError)
        {
            return results.First(result => result.IsFailure).Error;
        }

        return results
            .Select(result => result.Value)
            .ToList();
    }
}