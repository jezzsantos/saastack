using Application.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common.UnitTests;

[EntityName("aqueuename")]
public class TestQueuedMessage : IQueuedMessage
{
    public bool ABooleanValue { get; set; }

    public double ADoubleValue { get; set; }

    public string AStringProperty { get; set; } = null!;

    public string CallerId { get; set; } = null!;

    public string CallId { get; set; } = null!;

    public string? MessageId { get; set; } = "amessageid";

    public string? TenantId { get; set; }
}