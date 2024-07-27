using Common.Extensions;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IParameterFilter" /> that marks properties with information from annotations in the
///     <see cref="System.ComponentModel.DataAnnotations" /> namespace.
/// </summary>
[UsedImplicitly]
public class DataAnnotationsParameterFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        if (context.ParameterInfo.Exists())
        {
            var parameterInfo = context.ParameterInfo;
            var declaringType = parameterInfo.Member.DeclaringType;
            if (declaringType.IsAnnotatable())
            {
                parameter.SetRequired(parameterInfo);
                parameter.SetDescription(parameterInfo);
            }
        }
    }
}