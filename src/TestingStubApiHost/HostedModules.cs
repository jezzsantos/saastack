using Infrastructure.Web.Hosting.Common;

namespace TestingStubApiHost;

public static class HostedModules
{
    public static SubdomainModules Get()
    {
        var modules = new SubdomainModules();
#if TESTINGONLY
        modules.Register(new StubApiModule());
#endif

        return modules;
    }
}