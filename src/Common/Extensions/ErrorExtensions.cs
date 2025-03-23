using System.Reflection;

namespace Common.Extensions;

public static class ErrorExtensions
{
    /// <summary>
    ///     Throws an exception representing the error
    /// </summary>
    public static void Throw<TException>(this Error error)
        where TException : Exception
    {
        throw ToException<TException>(error);
    }

    /// <summary>
    ///     Returns an exception representing the error
    /// </summary>
    public static Exception ToException<TException>(this Error error)
        where TException : Exception
    {
        var message = error.Message.HasValue()
            ? $"{error.Code}: {error.Message}"
            : error.Code.ToString();

        var exception =
            (Exception)Activator.CreateInstance(typeof(TException), BindingFlags.Public, null,
                new object[] { message })!;
        return exception;
    }
}