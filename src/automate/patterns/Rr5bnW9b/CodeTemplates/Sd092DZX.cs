using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.{{Parent.SubdomainName | string.pascalplural}};

{{~if Kind == "Search"~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalplural}}Response : SearchResponse
{
    public List<{{Parent.SubdomainName | string.pascalsingular}}> {{Parent.SubdomainName | string.pascalplural}} { get; set; } = [];
}
{{~else~}}
public class {{ActionName}}{{Parent.SubdomainName | string.pascalsingular}}Response : IWebResponse
{
    public required {{Parent.SubdomainName | string.pascalsingular}} {{Parent.SubdomainName | string.pascalsingular}} { get; set; }
}
{{~end~}}