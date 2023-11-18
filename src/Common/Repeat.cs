namespace Common;

public static class Repeat
{
    /// <summary>
    ///     Executes the specified <see cref="action" /> <see cref="count" /> times over in a loop.
    /// </summary>
    public static void Times(Action action, int count)
    {
        Times(action, 0, count);
    }

    /// <summary>
    ///     Executes the specified <see cref="action" /> <see cref="count" /> times over in a loop.
    /// </summary>
    public static void Times(Action<int> action, int count)
    {
        Times(action, 0, count);
    }

    private static void Times(Action action, int from, int to)
    {
        var counter = Enumerable.Range(from, to).ToList();
        counter.ForEach(_ => { action(); });
    }

    private static void Times(Action<int> action, int from, int to)
    {
        var counter = Enumerable.Range(from, to).ToList();
        counter.ForEach(index => { action(index + 1); });
    }
}