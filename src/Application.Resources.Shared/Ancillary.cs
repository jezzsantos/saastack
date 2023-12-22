using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Audit : IIdentifiableResource
{
    public string? AgainstId { get; set; }

    public required string AuditCode { get; set; }

    public required string MessageTemplate { get; set; }

    public required string OrganizationId { get; set; }

    public required List<string> TemplateArguments { get; set; }

    public required string Id { get; set; }
}