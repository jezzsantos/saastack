using Common;

namespace Application.Services.Shared;

/// <summary>
///     Defines a service that provides subscribers of domain events notifications
/// </summary>
public interface IDomainEventingSubscriberService
{
    /// <summary>
    ///     Registers all subscribers to the message bus topic for consuming domain events
    /// </summary>
    Task<Result<Error>> RegisterAllSubscribersAsync(CancellationToken cancellationToken);
}