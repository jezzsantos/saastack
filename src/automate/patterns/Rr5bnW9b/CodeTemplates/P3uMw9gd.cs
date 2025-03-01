using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using {{SubdomainName | string.pascalplural}}Application;
using Common;

namespace {{SubdomainName | string.pascalplural}}Infrastructure.ApplicationServices;

public class {{SubdomainName | string.pascalplural}}InProcessServiceClient : I{{SubdomainName | string.pascalplural}}Service
{
    private readonly I{{SubdomainName | string.pascalplural}}Application _{{SubdomainName | string.pascalplural | string.downcase}}Application;

    public {{SubdomainName | string.pascalplural}}InProcessServiceClient(I{{SubdomainName | string.pascalplural}}Application {{SubdomainName | string.pascalsingular | string.downcase}}Application)
    {
        _{{SubdomainName | string.pascalplural | string.downcase}}Application = {{SubdomainName | string.pascalsingular | string.downcase}}Application;
    }

    //TODO: add other cross-domain methods

}