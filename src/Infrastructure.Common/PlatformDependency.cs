using Infrastructure.Interfaces;

namespace Infrastructure.Common;

/// <summary>
///     Provides a platform dependency
/// </summary>
public class PlatformDependency<TService> : IPlatformDependency<TService>
{
    private readonly TService _instance;

    public PlatformDependency(TService instance)
    {
        _instance = instance;
    }

    public TService UnWrap()
    {
        return _instance;
    }
}