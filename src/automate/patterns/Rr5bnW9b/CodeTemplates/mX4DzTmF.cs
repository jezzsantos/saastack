using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace {{SubdomainName | string.pascalplural}}Application;

public interface I{{SubdomainName | string.pascalplural}}Application
{
{{~for operation in ServiceOperation.Items~}}
{{if (operation.IsTestingOnly)}}#if TESTINGONLY{{end}}
{{~if (operation.Kind == "Post")~}}
    Task<Result<{{SubdomainName | string.pascalsingular}}, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, CancellationToken cancellationToken);
{{~end~}}{{~if (operation.Kind == "PutPatch")~}}
    Task<Result<{{SubdomainName | string.pascalsingular}}, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, string id, CancellationToken cancellationToken);
{{~end~}}{{~if (operation.Kind == "Get")~}}
    Task<Result<{{SubdomainName | string.pascalsingular}}, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, string id, CancellationToken cancellationToken);
{{~end~}}{{~if (operation.Kind == "Search")~}}
    Task<Result<SearchResults<{{SubdomainName | string.pascalsingular}}>, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalplural}}Async(ICallerContext caller, string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);
{{~end~}}{{~if (operation.Kind == "Delete")~}}
    Task<Result<Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, string id, CancellationToken cancellationToken);
{{~end~}}
{{if (operation.IsTestingOnly)}}#endif{{end}}
{{~end~}}
}