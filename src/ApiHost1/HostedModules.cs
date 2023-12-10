using BookingsInfrastructure;
using CarsInfrastructure;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

public static class HostedModules
{
    public static SubDomainModules Get()
    {
        // EXTEND: Add the sub domain of each API, to host in this project.
        // NOTE: The order of these registrations will matter for some dependencies 
        var modules = new SubDomainModules();
#if TESTINGONLY
        modules.Register(new TestingOnlyApiModule());
#endif
        modules.Register(new CarsModule());
        modules.Register(new BookingsModule());

        return modules;
    }
}