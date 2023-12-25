using Infrastructure.Web.Hosting.Common;

namespace WebsiteHost;

public static class HostedModules
{
    public static SubDomainModules Get()
    {
        var modules = new SubDomainModules();
        modules.Register(new BackEndForFrontEndModule());

        return modules;
    }
}