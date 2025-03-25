using EventNotificationsInfrastructure;
using Infrastructure.Web.Hosting.Common;

namespace ProjectName;

public static class HostedModules
{
    public static SubdomainModules Get()
    {
        var modules = new SubdomainModules();
        // EXTEND: Register a module for each subdomain, to be hosted in this project.
        // NOTE: The order of these registrations might matter for some dependencies 
        modules.Register(new EventNotificationsModule());
        modules.Register(new ASubdomainModule());
        
        return modules;
    }
}