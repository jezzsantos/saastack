namespace Common.Extensions;

public static class Tasks
{
    /// <summary>
    ///     Runs all the specified <see cref="tasks" /> and returns the first <see cref="Error" /> if any
    /// </summary>
    public static async Task<Result<Error>> WhenAllAsync(params Task<Result<Error>>[] tasks)
    {
        var results = await Task.WhenAll(tasks);
        return results.Any(x => x.IsFailure)
            ? results.First(x => x.IsFailure).Error
            : Result.Ok;
    }
}