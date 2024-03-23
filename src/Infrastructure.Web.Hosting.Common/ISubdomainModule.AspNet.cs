using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Hosting.Common;

partial interface ISubdomainModule
{
    /// <summary>
    ///     Returns a function that handles the request pipeline middleware configuration for this module
    /// </summary>
    Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware { get; }

    /// <summary>
    ///     Returns a function for registering additional dependencies into the dependency injection container
    /// </summary>
    Action<ConfigurationManager, IServiceCollection>? RegisterServices { get; }
}