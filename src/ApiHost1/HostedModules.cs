using CarsApi;
using Infrastructure.WebApi.Common;

namespace ApiHost1;

public static class HostedModules
{
    public static SubDomainModules Get()
    {
        // EXTEND: Add the sub domain of each API, to host in this project.
        // NOTE: The order of these registrations will matter for some dependencies 
        var modules = new SubDomainModules();
        modules.Register(new CarsApiModule());
        modules.Register(new TestingOnlyApiModule());

        return modules;
    }
}