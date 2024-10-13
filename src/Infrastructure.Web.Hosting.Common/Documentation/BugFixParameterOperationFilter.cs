using Common.Extensions;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that invokes the registered <see cref="IParameterFilter" />s for each of
///     the parameters of the operation.
///     This filter is a BUGFIX to Swashbuckle: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2613
/// </summary>
[UsedImplicitly]
public class BugFixParameterOperationFilter : IOperationFilter
{
    private readonly SwaggerGenOptions _generatorOptions;

    public BugFixParameterOperationFilter(SwaggerGenOptions generatorOptions)
    {
        _generatorOptions = generatorOptions;
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        foreach (var parameterFilter in _generatorOptions.ParameterFilterDescriptors)
        {
            var filter = (IParameterFilter)Activator.CreateInstance(parameterFilter.Type, parameterFilter.Arguments)!;
            foreach (var parameter in operation.Parameters)
            {
                var description =
                    context.ApiDescription.ParameterDescriptions.FirstOrDefault(x => x.Name == parameter.Name);
                if (description.NotExists())
                {
                    continue;
                }

                var parameterContext = new ParameterFilterContext(description, context.SchemaGenerator,
                    context.SchemaRepository, null, description.ParameterInfo());

                filter.Apply(parameter, parameterContext);
            }
        }
    }
}