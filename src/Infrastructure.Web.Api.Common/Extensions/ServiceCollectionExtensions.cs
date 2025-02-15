using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers every <see cref="IValidator{TRequest}" /> found in the specified
    ///     <see cref="assembliesContainingValidators" /> as scoped dependencies
    /// </summary>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IServiceCollection RegisterFluentValidators(this IServiceCollection services,
        IEnumerable<Assembly> assembliesContainingValidators)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembliesContainingValidators);

        AssemblyScanner.FindValidatorsInAssemblies(assembliesContainingValidators)
            .ForEach(result => { services.AddScoped(result.InterfaceType, result.ValidatorType); });

        return services;
    }
}