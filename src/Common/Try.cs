namespace Common;

public static class Try
{
    /// <summary>
    ///     Performs the specified <see cref="action" />, and if the action throws, then ignores the exception
    /// </summary>
    public static void Safely(Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            if (ex is StackOverflowException or OutOfMemoryException)
            {
                throw;
            }

            //Ignore exception!
        }
    }

    /// <summary>
    ///     Performs the specified <see cref="action" />, and if the action throws, then ignores the exception
    /// </summary>
    public static TReturn? Safely<TReturn>(Func<TReturn> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            if (ex is StackOverflowException or OutOfMemoryException)
            {
                throw;
            }

            //Ignore exception!
            return default;
        }
    }
}