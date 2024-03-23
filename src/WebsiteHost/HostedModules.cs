using Infrastructure.Web.Hosting.Common;

namespace WebsiteHost;

public static class HostedModules
{
    public static SubdomainModules Get()
    {
        var modules = new SubdomainModules();
        modules.Register(new BackEndForFrontEndModule());

        return modules;
    }
}