using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Identities.APIKeys;

public sealed class ParametersChanged : DomainEvent
{
    public ParametersChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ParametersChanged()
    {
    }

    public required string Description { get; set; }

    public required DateTime ExpiresOn { get; set; }
}