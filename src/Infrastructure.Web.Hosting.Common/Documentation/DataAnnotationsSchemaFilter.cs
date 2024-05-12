using Common.Extensions;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="ISchemaFilter" /> that processes schema components and their properties
///     It changes the schema from annotations from the <see cref="System.ComponentModel.DataAnnotations" />
///     namespace, that are placed on properties of the request and response types.
/// </summary>
[UsedImplicitly]
public class DataAnnotationsSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.MemberInfo.Exists())
        {
            // we deal with each of the properties of a schema component
            var member = context.MemberInfo;
            var declaringType = member.DeclaringType;
            if (declaringType.IsAnnotatable())
            {
                schema.SetDescription(member);
            }

            return;
        }

        if (context.ParameterInfo.Exists())
        {
            // we deal with parameters in the DataAnnotationsParameterFilter , not here
            return;
        }

        if (context.Type.IsAnnotatable())
        {
            // we deal with the schema component
            var requestDto = context.Type;
            var properties = requestDto.GetProperties();
            foreach (var property in properties)
            {
                if (property.IsPropertyRequired())
                {
                    var name = property.Name.ToCamelCase();
                    var required = schema.Required ?? new HashSet<string>();
                    // ReSharper disable once PossibleUnintendedLinearSearchInSet
                    if (!required.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        required.Add(name);
                    }
                }
            }
        }
    }
}