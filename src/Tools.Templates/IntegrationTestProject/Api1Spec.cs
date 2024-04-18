using ProjectName;
using ApiHost1;
using FluentAssertions;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace ProjectName;

[Trait("Category", "Integration.API")] [Collection("API")]
public class Api1Spec : WebApiSpec<Program>
{
    public Api1Spec(WebApiSetup<Program> setup) : base(setup, OverrideDependencies)
    {
        EmptyAllRepositories();
    }

    private static void OverrideDependencies(IServiceCollection services)
    {
        //TODO: remove this is method if you are not overriding any dependencies with any stubs
        throw new NotImplementedException();
    }

    //TIP: type testm or testma to create a new test method
}