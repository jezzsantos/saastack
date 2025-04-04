namespace Common;

/// <summary>
///     Defines an interface for accessing environment variables.
/// </summary>
public interface IEnvironmentVariables
{
    /// <summary>
    ///     Fetches the specified environment variable.
    /// </summary>
    Optional<string> Get(string name);
}