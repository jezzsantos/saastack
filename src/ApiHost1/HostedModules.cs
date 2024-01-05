using AncillaryInfrastructure;
using BookingsInfrastructure;
using CarsInfrastructure;
using EndUsersInfrastructure;
using IdentityInfrastructure;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

public static class HostedModules
{
    public static SubDomainModules Get()
    {
        var modules = new SubDomainModules();
        modules.Register(new ApiHostModule());
        modules.Register(new EndUsersModule());
        modules.Register(new IdentityModule());
        modules.Register(new AncillaryModule());
#if TESTINGONLY
        modules.Register(new TestingOnlyApiModule());
#endif
        // EXTEND: Register a module for each subdomain, to be hosted in this project.
        // NOTE: The order of these registrations might matter for some dependencies 
        modules.Register(new CarsModule());
        modules.Register(new BookingsModule());

        return modules;
    }
}