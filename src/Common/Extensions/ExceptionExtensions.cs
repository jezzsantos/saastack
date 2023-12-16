namespace Common.Extensions;

public static class ExceptionExtensions
{
    /// <summary>
    ///     Creates an error from the specified <see cref="ex" />, with the specified <see cref="code" />
    /// </summary>
    public static Error ToError(this Exception ex, ErrorCode code)
    {
        return new Error(code, ex.Message);
    }
}