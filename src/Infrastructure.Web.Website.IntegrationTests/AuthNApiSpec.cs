#if TESTINGONLY
using IntegrationTesting.WebApi.Common;
using WebsiteHost;
using Xunit;

namespace Infrastructure.Web.Website.IntegrationTests;

[Trait("Category", "Integration.Web")]
[Collection("API")]
public class AuthNApiSpec : WebApiSpec<Program>
{
    public AuthNApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    //TODO: tests to check cookie authenticated endpoints
}
#endif