namespace IdentityApplication.ApplicationServices;

public class DelayGenerator : IDelayGenerator
{
    private static readonly Random Random = Random.Shared;

    public TimeSpan GetNextRandom(double fromSeconds, double toSeconds)
    {
        return TimeSpan.FromMilliseconds(Random.Next((int)(fromSeconds * 1000), (int)(toSeconds * 1000)));
    }
}