using System.Net;
using ProjectName;
using FluentAssertions;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace ProjectName;

[Trait("Category", "Integration.Web")]
public class Api1Spec : WebApiSpec<Program>
{
    public Api1Spec(WebApiSetup<Program> setup) : base(setup)
    {
    }
    
    //TODO: type testm or testma to create a new test method
}