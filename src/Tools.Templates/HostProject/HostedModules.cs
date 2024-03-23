using Infrastructure.WebApi.Common;

namespace ProjectName;

public static class HostedModules
{
    public static SubDomainModules Get()
    {
        // EXTEND: Add the subdomain of each API, to host in this project.
        // NOTE: The order of these registrations will matter for some dependencies 
        var modules = new SubDomainModules();

        return modules;
    }
}