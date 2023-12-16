using Infrastructure.Web.Hosting.Common;

namespace TestingStubApiHost;

public static class HostedModules
{
    public static SubDomainModules Get()
    {
        var modules = new SubDomainModules();
#if TESTINGONLY
        modules.Register(new StubApiModule());
#endif

        return modules;
    }
}