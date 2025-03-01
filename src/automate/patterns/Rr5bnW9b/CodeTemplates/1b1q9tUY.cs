using System.Net;
using ApiHost1;
using Application.Resources.Shared;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.{{SubdomainName | string.pascalplural}};
using Infrastructure.Web.Common.Extensions;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace {{SubdomainName | string.pascalplural}}Infrastructure.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class {{SubdomainName | string.pascalplural}}ApiSpec : WebApiSpec<Program>
{
    public {{SubdomainName | string.pascalplural}}ApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    //TODO: Write Integration tests for each of the APIs

    private static void OverrideDependencies(IServiceCollection services)
    {
        // do nothing
    }
    
}