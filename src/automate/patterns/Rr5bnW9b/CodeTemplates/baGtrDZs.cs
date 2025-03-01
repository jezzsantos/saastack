using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class {{SubdomainName | string.pascalsingular}} : IIdentifiableResource
{
    public required string Id { get; set; }
    
    //TODO: add other resource fields here
}