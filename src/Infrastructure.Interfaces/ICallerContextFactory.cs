using Application.Interfaces;

namespace Infrastructure.Interfaces;

/// <summary>
///     Defines a factory to retrieve instances of the <see cref="ICallerContext" />
/// </summary>
public interface ICallerContextFactory
{
    ICallerContext Create();
}