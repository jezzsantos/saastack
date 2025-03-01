using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.{{Parent.SubdomainName | string.pascalplural}};

/// <summary>
///     {{ActionName}} a {{Parent.SubdomainName | string.pascalsingular | string.downcase}}
/// </summary>
[Route("{{Route | string.downcase}}", OperationMethod.{{Kind}}, AccessType.{{IsAuthorized ? 'Token' : 'Anonymous'}})]
{{~if IsAuthorized~}}
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
{{~end~}}
{{~if Kind == "Search"~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}Request : TenantedSearchRequest<{{ActionName}}{{Parent.SubdomainName | string.pascalplural}}Request, {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}Response>
{
    //TODO: add other filter fields here, and annotate with the [Required] attribute if they are not optional
}
{{~else~}}{{~if Kind == "Delete"~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request : TenantedDeleteRequest<{{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request>
{
    [Required] public string? Id { get; set; }
}
{{~else~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request : TenantedRequest<{{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Request, {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Response>
{
    //TODO: add other fields here, and annotate with the [Required] attribute if they are not optional
{{~if Kind == "Get" || Kind == "PutPatch"~}}    
    [Required] public string? Id { get; set; }
{{~end~}}
}
{{~end~}}
{{~end~}}