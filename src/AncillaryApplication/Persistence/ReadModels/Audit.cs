using AncillaryDomain;
using Application.Persistence.Common;
using Common;
using QueryAny;

namespace AncillaryApplication.Persistence.ReadModels;

[EntityName("Audit")]
public class Audit : ReadModelEntity
{
    public Optional<string> AgainstId { get; set; }

    public Optional<string> AuditCode { get; set; }

    public Optional<DateTime?> Created { get; set; }

    public Optional<string> MessageTemplate { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public TemplateArguments TemplateArguments { get; set; } = TemplateArguments.Create().Value;
}