using Application.Resources.Shared;
using {{SubdomainName | string.pascalplural}}Application;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.{{SubdomainName | string.pascalplural}};

namespace {{SubdomainName | string.pascalplural}}Infrastructure.Api.{{SubdomainName | string.pascalplural}};

public sealed class {{SubdomainName | string.pascalplural}}Api : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly I{{SubdomainName | string.pascalplural}}Application _{{SubdomainName | string.pascalplural | string.downcase}}Application;

    public {{SubdomainName | string.pascalplural}}Api(ICallerContextFactory callerFactory, I{{SubdomainName | string.pascalplural}}Application {{SubdomainName | string.pascalplural | string.downcase}}Application)
    {
        _callerFactory = callerFactory;
        _{{SubdomainName | string.pascalplural | string.downcase}}Application = {{SubdomainName | string.pascalplural | string.downcase}}Application;
    }

{{~for operation in ServiceOperation.Items~}}
{{if (operation.IsTestingOnly)}}#if TESTINGONLY{{end}}
{{~if (operation.Kind == "Post")~}}
    public async Task<ApiPostResult<{{SubdomainName | string.pascalsingular}}, {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response>> {{operation.ActionName}}({{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Request request, CancellationToken cancellationToken)
    {
        var {{SubdomainName | string.pascalsingular | string.downcase}} = await _{{SubdomainName | string.pascalplural | string.downcase}}Application.{{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(_callerFactory.Create(), request.OrganizationId!, cancellationToken);

        return () => {{SubdomainName | string.pascalsingular | string.downcase}}.HandleApplicationResult<{{SubdomainName | string.pascalsingular}}, {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response>(c =>
            new PostResult<{{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response>(new {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response { {{SubdomainName | string.pascalsingular}} = c }, new Get{{SubdomainName | string.pascalsingular}}Request { Id = c.Id }.ToUrl()));
    }
{{~end~}}{{~if (operation.Kind == "PutPatch")~}}
    public async Task<ApiPutPatchResult<{{SubdomainName | string.pascalsingular}}, {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response>> {{operation.ActionName}}({{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Request request, CancellationToken cancellationToken)
    {
        var {{SubdomainName | string.pascalsingular | string.downcase}} = await _{{SubdomainName | string.pascalplural | string.downcase}}Application.{{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(_callerFactory.Create(), request.OrganizationId!, request.Id!, cancellationToken);

        return () => {{SubdomainName | string.pascalsingular | string.downcase}}.HandleApplicationResult(c => new {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response { {{SubdomainName | string.pascalsingular}} = c });
    }
{{~end~}}{{~if (operation.Kind == "Get")~}}
    public async Task<ApiGetResult<{{SubdomainName | string.pascalsingular}}, {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response>> {{operation.ActionName}}({{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Request request, CancellationToken cancellationToken)
    {
        var {{SubdomainName | string.pascalsingular | string.downcase}} = await _{{SubdomainName | string.pascalplural | string.downcase}}Application.{{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(_callerFactory.Create(), request.OrganizationId!, request.Id!, cancellationToken);

        return () => {{SubdomainName | string.pascalsingular | string.downcase}}.HandleApplicationResult(c => new {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Response { {{SubdomainName | string.pascalsingular}} = c });
    }
{{~end~}}{{~if (operation.Kind == "Search")~}}
    public async Task<ApiSearchResult<{{SubdomainName | string.pascalsingular}}, {{operation.ActionName}}{{SubdomainName | string.pascalplural}}Response>> {{operation.ActionName}}({{operation.ActionName}}{{SubdomainName | string.pascalplural}}Request request, CancellationToken cancellationToken)
    {
        var {{SubdomainName | string.pascalplural | string.downcase}} = await _{{SubdomainName | string.pascalplural | string.downcase}}Application.{{operation.ActionName}}{{SubdomainName | string.pascalplural}}Async(_callerFactory.Create(), request.OrganizationId!, request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            {{SubdomainName | string.pascalplural | string.downcase}}.HandleApplicationResult(c => new {{operation.ActionName}}{{SubdomainName | string.pascalplural}}Response { {{SubdomainName | string.pascalplural}} = c.Results, Metadata = c.Metadata });
    }
{{~end~}}{{~if (operation.Kind == "Delete")~}}
    public async Task<ApiDeleteResult> {{operation.ActionName}}({{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Request request, CancellationToken cancellationToken)
    {
        var {{SubdomainName | string.pascalsingular | string.downcase}} = await _{{SubdomainName | string.pascalplural | string.downcase}}Application.{{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(_callerFactory.Create(), request.OrganizationId!, request.Id!, cancellationToken);

        return () => {{SubdomainName | string.pascalsingular | string.downcase}}.HandleApplicationResult();
    }
{{~end~}}
{{if (operation.IsTestingOnly)}}#endif{{end}}

{{~end~}}
}