using ApiHost1;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityInfrastructure.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class MachineCredentialsApiSpec : WebApiSpec<Program>
{
    public MachineCredentialsApiSpec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories(setup);
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //TODO: remove this is method if you are not overriding any dependencies with any stubs
        throw new NotImplementedException();
    }

    //TIP: type testm or testma to create a new test method
}