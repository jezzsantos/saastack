namespace Common.Extensions;

public static class ResultExtensions
{
    /// <summary>
    ///     Throws an exception representing the error, if there is one
    /// </summary>
    public static void ThrowOnError<TValue>(this Result<TValue, Error> result)
    {
        if (result.IsFailure)
        {
            result.Error.Throw();
        }
    }

    /// <summary>
    ///     Throws an exception representing the error, if there is one
    /// </summary>
    public static void ThrowOnError(this Result<Error> result)
    {
        if (result.IsFailure)
        {
            result.Error.Throw();
        }
    }
}