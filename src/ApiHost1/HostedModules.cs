using AncillaryInfrastructure;
using BookingsInfrastructure;
using CarsInfrastructure;
using EndUsersInfrastructure;
using IdentityInfrastructure;
using ImagesInfrastructure;
using Infrastructure.Web.Hosting.Common;
using OrganizationsInfrastructure;
using UserProfilesInfrastructure;

namespace ApiHost1;

public static class HostedModules
{
    public static SubdomainModules Get()
    {
        var modules = new SubdomainModules();
        modules.Register(new ApiHostModule());
        modules.Register(new ImagesModule());
        modules.Register(new UserProfilesModule());
        modules.Register(new EndUsersModule());
        modules.Register(new OrganizationsModule());
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