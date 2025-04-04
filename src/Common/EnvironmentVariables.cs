using Common.Extensions;

namespace Common;

/// <summary>
///     Provides access to environment variables.
/// </summary>
public class EnvironmentVariables : IEnvironmentVariables
{
    public Optional<string> Get(string name)
    {
        var result = Try.Safely(() => Environment.GetEnvironmentVariable(name));
        if (result.HasValue())
        {
            return result.ToOptional();
        }

        return Optional<string>.None;
    }
}