using Domain.Shared.EndUsers;
using Infrastructure.Eventing.Common.Notifications;
using JetBrains.Annotations;

namespace Integration.Events.Shared.EndUsers;

public sealed class PersonRegistered : IntegrationEvent
{
    public PersonRegistered(string id) : base(id)
    {
    }

    [UsedImplicitly]
    public PersonRegistered()
    {
    }

    public required List<string> Features { get; set; }

    public required List<string> Roles { get; set; }

    public required string Username { get; set; }

    public required RegisteredUserProfile UserProfile { get; set; }
}