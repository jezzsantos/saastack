using Application.Interfaces;
using Application.Persistence.Interfaces;
using {{SubdomainName | string.pascalplural}}Application.Persistence.ReadModels;
using {{SubdomainName | string.pascalplural}}Domain;
using Common;
using Domain.Common.ValueObjects;

namespace {{SubdomainName | string.pascalplural}}Application.Persistence;

public interface I{{SubdomainName | string.pascalsingular}}Repository : IApplicationRepository
{
    Task<Result<{{SubdomainName | string.pascalsingular}}Root, Error>> LoadAsync(Identifier organizationId, Identifier id, CancellationToken cancellationToken);

    Task<Result<{{SubdomainName | string.pascalsingular}}Root, Error>> SaveAsync({{SubdomainName | string.pascalsingular}}Root {{SubdomainName | string.pascalsingular | string.downcase}}, bool reload, CancellationToken cancellationToken);

    Task<Result<{{SubdomainName | string.pascalsingular}}Root, Error>> SaveAsync({{SubdomainName | string.pascalsingular}}Root {{SubdomainName | string.pascalsingular | string.downcase}}, CancellationToken cancellationToken);
}