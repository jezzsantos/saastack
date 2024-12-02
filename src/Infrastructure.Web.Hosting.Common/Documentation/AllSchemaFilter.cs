using Common.Extensions;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="ISchemaFilter" /> that processes schema components and their properties
///     It modifies all schema, based on representation and on annotations from the
///     <see cref="System.ComponentModel.DataAnnotations" /> namespace.
/// </summary>
[UsedImplicitly]
public class AllSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.MemberInfo.Exists())
        {
            var member = context.MemberInfo;
            var declaringType = member.DeclaringType;
            if (declaringType.IsRequestType())
            {
                // dealing with each property of a request type
                schema.SetPropertyDescription(member);

                return;
            }

            if (declaringType.IsResponseType())
            {
                // dealing with each property of a responses type
                schema.SetPropertyDescription(member);
                schema.SetPropertyNullable(member);

                return;
            }

            if (declaringType.Exists())
            {
                schema.SetPropertyNullable(member);
            }

            return;
        }

        if (context.ParameterInfo.Exists())
        {
            // Parameters are dealt with in the DataAnnotationsParameterFilter , not here
            return;
        }

        // dealing with any other schemas in general
        var dtoType = context.Type;
        if (context.Type.IsRequestOrResponseType())
        {
            schema.CollateRequiredProperties(dtoType);
            schema.RemoveRouteTemplateFields(dtoType);
        }
        else
        {
            schema.CollateRequiredProperties(dtoType);
        }

        if (context.Type.IsEnum)
        {
            schema.SetEnumValues(context.Type);
        }
    }
}