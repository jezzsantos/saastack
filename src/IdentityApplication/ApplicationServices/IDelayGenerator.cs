namespace IdentityApplication.ApplicationServices;

public interface IDelayGenerator
{
    TimeSpan GetNextRandom(double fromSeconds, double toSeconds);
}