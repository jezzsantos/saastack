using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.WebApi.Common;

public interface ISubDomainModule
{
    public Assembly ApiAssembly { get; }

    public Action<WebApplication> MinimalApiRegistrationFunction { get; }
    public Action<ConfigurationManager, IServiceCollection>? RegisterServicesFunction { get; }
}