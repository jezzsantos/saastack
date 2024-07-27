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
            var member = context.MemberInfo;
            var declaringType = member.DeclaringType;
            if (declaringType.IsAnnotatable())
            {
                // we only deal with each member of a request and responses types
                schema.SetDescription(member);
            }

            return;
        }

        if (context.ParameterInfo.Exists())
        {
            // Parameters are dealt with in the DataAnnotationsParameterFilter , not here
            return;
        }

        // we deal with other schemas in general
        if (context.Type.IsAnnotatable())
        {
            var requestDto = context.Type;
            var properties = requestDto.GetProperties();
            foreach (var property in properties)
            {
                // we have to add all required properties to the request collection
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

        if (context.Type.IsEnum)
        {
            schema.SetEnumValues(context.Type);
        }
    }
}