using Application.Persistence.Common;
using Common;
using OrganizationsDomain;
using QueryAny;

namespace OrganizationsApplication.Persistence.ReadModels;

[EntityName("Organization")]
public class Organization : ReadModelEntity
{
    public Optional<string> CreatedById { get; set; }

    public Optional<string> Name { get; set; }

    public Optional<Ownership> Ownership { get; set; }
}