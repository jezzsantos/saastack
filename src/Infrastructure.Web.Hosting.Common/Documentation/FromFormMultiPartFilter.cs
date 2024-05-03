using System.Reflection;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that fixes the Swagger UI to display tooling for a request that is
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

        var requestProperties = parameter.Value.Type.GetProperties()
            .Select(prop => prop)
            .ToList();
        foreach (var property in requestProperties)
        {
            parts.Add(property.Name, new OpenApiSchema
            {
                Type = ConvertToSchemaType(context, property)
            });
            var isRequired = IsPropertyRequired(property);
            if (isRequired)
            {
                requiredParts.Add(property.Name);
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

    private static bool IsPropertyRequired(PropertyInfo property)
    {
        var nullabilityContext = new NullabilityInfoContext();
        var nullability = nullabilityContext.Create(property);

        return nullability.ReadState == NullabilityState.NotNull;
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

        if (!RequestHasBody(context))
        {
            return Optional<ApiParameterDescription>.None;
        }

        return requestParameters.First();

        static bool RequestHasBody(OperationFilterContext context)
        {
            var method = context.ApiDescription.HttpMethod;
            return method == HttpMethod.Post.Method
                   || method == HttpMethod.Put.Method
                   || method == HttpMethod.Patch.Method;
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