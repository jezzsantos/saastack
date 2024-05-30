namespace Common.Extensions;

public static class ErrorExtensions
{
    /// <summary>
    ///     Throws an exception representing the error
    /// </summary>
    public static void Throw(this Error error)
    {
        var message = error.Message.HasValue()
            ? $"{error.Code}: {error.Message}"
            : error.Code.ToString();

        throw new InvalidOperationException(message);
    }
}