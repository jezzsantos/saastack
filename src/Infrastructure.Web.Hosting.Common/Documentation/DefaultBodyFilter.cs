using System.Reflection;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that adds the request body schemas for all POST, PUT and PATCH requests,
///     and adds additional Swagger UI to display extra tooling for <see cref="IHasMultipartForm" /> requests.
/// </summary>
[UsedImplicitly]
public sealed class DefaultBodyFilter : IOperationFilter
{
    internal const string FormFilesFieldName = "files";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var type = context.GetRequestType();
        if (!type.HasValue)
        {
            return;
        }

        if (!RequestHasBody(context))
        {
            return;
        }

        var requestDto = type.Value;
        var requestProperties = requestDto.GetProperties();
        var content = new Dictionary<string, OpenApiMediaType>();

        var isMultiPartForm = typeof(IHasMultipartForm).IsAssignableFrom(type);
        if (isMultiPartForm)
        {
            var parts = new Dictionary<string, OpenApiSchema>
            {
                {
                    FormFilesFieldName, new OpenApiSchema
                    {
                        Type = "array",
                        Items = new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary"
                        }
                    }
                }
            };
            var requiredParts = new HashSet<string>
            {
                FormFilesFieldName
            };

            foreach (var property in requestProperties)
            {
                if (property.IsPropertyInRoute())
                {
                    continue;
                }

                var name = property.Name.ToCamelCase();
                parts.Add(name, new OpenApiSchema
                {
                    Type = ConvertToSchemaType(context, property)
                });

                if (property.IsPropertyRequired())
                {
                    requiredParts.Add(name);
                }
            }

            content.Add(HttpConstants.ContentTypes.MultiPartFormData, new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = parts,
                    Required = requiredParts
                }
            });
        }
        else
        {
            content.Add(HttpConstants.ContentTypes.Json, new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Reference = new OpenApiReference
                    {
                        Id = context.SchemaGenerator.GenerateSchema(requestDto, context.SchemaRepository).Reference.Id,
                        Type = ReferenceType.Schema
                    }
                }
            });
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = content
        };
    }

    /// <summary>
    ///     Converts the <see cref="field" /> into the respective schema type.
    /// </summary>
    private static string ConvertToSchemaType(OperationFilterContext context, PropertyInfo field)
    {
        var type = context.SchemaGenerator.GenerateSchema(field.PropertyType, context.SchemaRepository);
        return type.Type;
    }

    private static bool RequestHasBody(OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;
        var httpMethod = new HttpMethod(method ?? HttpMethod.Get.Method);

        return httpMethod.CanHaveBody();
    }
}