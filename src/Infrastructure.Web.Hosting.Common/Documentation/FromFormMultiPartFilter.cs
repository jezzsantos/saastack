using System.Reflection;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that fixes the Swagger UI to display tooling for operations that are
///     marked as <see cref="Microsoft.AspNetCore.Mvc.FromFormAttribute" />,
///     that was source generated from a <see cref="IHasMultipartForm" /> request.
/// </summary>
[UsedImplicitly]
public sealed class FromFormMultiPartFilter : IOperationFilter
{
    internal const string FormFilesFieldName = "files";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameter = IsMultiPartFormRequest(context);
        if (!parameter.HasValue)
        {
            return;
        }

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

        var requestDto = parameter.Value.Type;
        var requestProperties = requestDto.GetProperties();
        foreach (var property in requestProperties)
        {
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

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                [HttpConstants.ContentTypes.MultiPartFormData] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = parts,
                        Required = requiredParts
                    }
                }
            }
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

    private static Optional<ApiParameterDescription> IsMultiPartFormRequest(OperationFilterContext context)
    {
        var requestParameters = context.ApiDescription.ParameterDescriptions
            .Where(IsFromFormRequest)
            .ToList();
        if (requestParameters.HasNone())
        {
            return Optional<ApiParameterDescription>.None;
        }

        if (!RequestCouldHaveBody(context))
        {
            return Optional<ApiParameterDescription>.None;
        }

        return requestParameters.First();

        static bool RequestCouldHaveBody(OperationFilterContext context)
        {
            var method = context.ApiDescription.HttpMethod;
            var httpMethod = new HttpMethod(method ?? HttpMethod.Get.Method);

            return httpMethod.CanHaveBody();
        }
        
        static bool IsFromFormRequest(ApiParameterDescription requestParameter)
        {
            var type = requestParameter.Type;
            if (type.NotExists())
            {
                return false;
            }

            var source = requestParameter.Source;
            if (source != BindingSource.FormFile)
            {
                return false;
            }

            return typeof(IWebRequest).IsAssignableFrom(type)
                   && typeof(IHasMultipartForm).IsAssignableFrom(type);
        }
    }
}