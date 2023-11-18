namespace Common.Extensions;

public static class Task
{
    /// <summary>
    ///     Runs all the specified <see cref="tasks" /> and returns the first <see cref="Error" /> if any
    /// </summary>
    public static async Task<Result<Error>> WhenAllAsync(params Task<Result<Error>>[] tasks)
    {
        var results = await System.Threading.Tasks.Task.WhenAll(tasks);
        return results.Any(x => !x.IsSuccessful)
            ? results.First(x => !x.IsSuccessful).Error
            : Result.Ok;
    }
}