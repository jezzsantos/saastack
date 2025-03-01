using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using {{SubdomainName | string.pascalplural}}Application.Persistence;
using {{SubdomainName | string.pascalplural}}Domain;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Unavailability = Application.Resources.Shared.Unavailability;

namespace {{SubdomainName | string.pascalplural}}Application;

public class {{SubdomainName | string.pascalplural}}Application : I{{SubdomainName | string.pascalplural}}Application
{
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly I{{SubdomainName | string.pascalsingular}}Repository _repository;

    public {{SubdomainName | string.pascalplural}}Application(IRecorder recorder, IIdentifierFactory idFactory, I{{SubdomainName | string.pascalsingular}}Repository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _repository = repository;
    }

{{~for operation in ServiceOperation.Items~}}
{{if (operation.IsTestingOnly)}}#if TESTINGONLY{{end}}
{{~if (operation.Kind == "Post")~}}
    public async Task<Result<{{SubdomainName | string.pascalsingular}}, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, CancellationToken cancellationToken)
{{~end~}}{{~if (operation.Kind == "PutPatch")~}}
    public async Task<Result<{{SubdomainName | string.pascalsingular}}, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, string id, CancellationToken cancellationToken)
{{~end~}}{{~if (operation.Kind == "Get")~}}
    public async Task<Result<{{SubdomainName | string.pascalsingular}}, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, string id, CancellationToken cancellationToken)
{{~end~}}{{~if (operation.Kind == "Search")~}}
    public async Task<Result<SearchResults<{{SubdomainName | string.pascalsingular}}>, Error>> {{operation.ActionName}}{{SubdomainName | string.pascalplural}}Async(ICallerContext caller, string organizationId, SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken)
{{~end~}}{{~if (operation.Kind == "Delete")~}}
    public async Task<Result<Error>> {{operation.ActionName}}{{SubdomainName | string.pascalsingular}}Async(ICallerContext caller, string organizationId, string id, CancellationToken cancellationToken)
{{~end~}}
    {
        throw new NotImplementedException();
    }
{{if (operation.IsTestingOnly)}}#endif{{end}}
{{~end~}}
}

internal static class {{SubdomainName | string.pascalsingular}}ConversionExtensions
{
    public static {{SubdomainName | string.pascalsingular}} To{{SubdomainName | string.pascalsingular}}(this {{SubdomainName | string.pascalsingular}}Root {{SubdomainName | string.pascalsingular | string.downcase}})
    {
        return new {{SubdomainName | string.pascalsingular}}
        {
            Id = {{SubdomainName | string.pascalsingular | string.downcase}}.Id,
            //TODO: add assignments to all other properties here
        };
    }
}