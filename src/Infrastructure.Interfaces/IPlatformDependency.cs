namespace Infrastructure.Interfaces;

/// <summary>
///     Defines a dependency that can be registered in the container, intended for platform use only
/// </summary>
public interface IPlatformDependency<out TService>
{
    TService UnWrap();
}