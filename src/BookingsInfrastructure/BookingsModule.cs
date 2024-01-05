using System.Reflection;
using BookingsApplication;
using BookingsApplication.Persistence;
using BookingsDomain;
using BookingsInfrastructure.Api.Bookings;
using BookingsInfrastructure.Persistence;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingsInfrastructure;

public class BookingsModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(BookingsApi).Assembly;

    public Assembly DomainAssembly => typeof(BookingRoot).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(BookingRoot), "booking" }
    };

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.RegisterTenanted<IBookingsApplication, BookingsApplication.BookingsApplication>();
                services.RegisterTenanted<IBookingRepository, BookingRepository>();
                services.RegisterTenantedEventing<BookingRoot>();
            };
        }
    }

    public Action<WebApplication> ConfigureMiddleware
    {
        get { return app => app.RegisterRoutes(); }
    }
}