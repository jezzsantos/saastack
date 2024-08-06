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
            if (declaringType.IsRequestOrResponseType())
            {
                // dealing with each property of a request and responses type
                schema.SetPropertyDescription(member);
            }

            return;
        }

        if (context.ParameterInfo.Exists())
        {
            // Parameters are dealt with in the DataAnnotationsParameterFilter , not here
            return;
        }

        // dealing with any other schemas in general
        if (context.Type.IsRequestOrResponseType())
        {
            var requestType = context.Type;
            schema.CollateRequiredProperties(requestType);
            schema.RemoveRouteTemplateFields(requestType);
        }

        if (context.Type.IsEnum)
        {
            schema.SetEnumValues(context.Type);
        }
    }
}