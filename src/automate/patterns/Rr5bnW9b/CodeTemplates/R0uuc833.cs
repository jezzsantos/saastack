using Application.Persistence.Common;
using {{SubdomainName | string.pascalplural}}Domain;
using Common;
using QueryAny;

namespace {{SubdomainName | string.pascalplural}}Application.Persistence.ReadModels;

[EntityName("{{SubdomainName | string.pascalsingular}}")]
public class {{SubdomainName | string.pascalsingular}} : ReadModelEntity
{
    public Optional<string> OrganizationId { get; set; }

    //TODO: Add other read model fields
}