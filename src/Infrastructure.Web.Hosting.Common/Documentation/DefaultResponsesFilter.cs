using System.Reflection;
using Common;
using Common.Extensions;
using FluentValidation.Results;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

/// <summary>
///     Provides a <see cref="IOperationFilter" /> that adds the responses for any request.
///     As well as building the default response for the given request type, this filter also inspects the responses that
///     are source-generated from the declaration of the respective <see cref="IWebRequest{TResponse}" />, and adds other
///     responses that are not defined in the source code.
/// </summary>
[UsedImplicitly]
public sealed class DefaultResponsesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var parameter = GetRequestType(context);
        if (!parameter.HasValue)
        {
            return;
        }

        var requestType = parameter.Value;
        operation.Responses = BuildResponses(context, operation.Responses, requestType);
    }

    private static OpenApiResponses BuildResponses(OperationFilterContext context, OpenApiResponses existingResponses,
        Type requestType)
    {
        var responseType = ConvertRequestTypeToResponseType(requestType);
        var defaultResponseContent = ConvertResponseTypeToContent(context, responseType);
        var method = HttpMethod.Parse(context.ApiDescription.HttpMethod ?? HttpMethod.Get.Method);
        var options = new ResponseCodeOptions(false, true);
        var defaultResponseCode = method.GetDefaultResponseCode(options);

        var defaultResponseDescription =
            GetDefaultResponseDescription(existingResponses, defaultResponseCode.ToStatusCode());
        var defaultResponse = new Dictionary<string, OpenApiResponse>
        {
            {
                defaultResponseCode.ToStatusCode().Numeric.ToString(), new OpenApiResponse
                {
                    Description = defaultResponseDescription,
                    Content = defaultResponseContent
                }
            }
        }.ToList();

        var validationErrors = new Dictionary<string, OpenApiResponse>
        {
            {
                StatusCode.BadRequest.Numeric.ToString(), new OpenApiResponse
                {
                    Description =
                        $"{StatusCode.BadRequest.Title}: {StatusCode.BadRequest.Reason}",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        [HttpConstants.ContentTypes.JsonProblem] = new()
                        {
                            Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails),
                                context.SchemaRepository),
                            Examples = new Dictionary<string, OpenApiExample>
                            {
                                {
                                    ErrorCode.Validation.ToString(), new OpenApiExample
                                    {
                                        Description = Resources
                                            .DefaultResponsesFilter_Example_ValidationError_Description,
                                        Summary = Resources.DefaultResponsesFilter_Example_ValidationError_Summary,
                                        Value = new OpenApiString(new ValidationResult(new List<ValidationFailure>
                                            {
                                                new("FieldA", "The error for FieldA")
                                                    { ErrorCode = "The Code for FieldA" },
                                                new("FieldB", "The error for FieldB")
                                                    { ErrorCode = "The Code for FieldB" }
                                            })
                                            .ToRfc7807("https://api.server.com/resource/123")
                                            .ToJson(true, StringExtensions.JsonCasing.Camel))
                                    }
                                },
                                {
                                    ErrorCode.RuleViolation.ToString(), new OpenApiExample
                                    {
                                        Description = Resources
                                            .DefaultResponsesFilter_Example_RuleViolationError_Description,
                                        Summary = Resources.DefaultResponsesFilter_Example_RuleViolationError_Summary,
                                        Value = new OpenApiString(Error.RuleViolation("A description of the violation")
                                            .ToProblem().ProblemDetails.ToJson(true, StringExtensions.JsonCasing.Camel))
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }.ToList();

        var generalErrors = HttpConstants.StatusCodes.SupportedErrorStatuses
            .Where(pair => pair.Key != StatusCode.BadRequest.Code)
            .Select(pair => pair)
            .OrderBy(pair => pair.Value.Numeric)
            .ToDictionary(pair => pair.Value.Numeric.ToString(), pair => new OpenApiResponse
            {
                Description = $"{pair.Value.Title}: {pair.Value.Reason}"
            });

        generalErrors.Add(StatusCode.InternalServerError.Numeric.ToString(), new OpenApiResponse
        {
            Description = $"{StatusCode.InternalServerError.Title}: {StatusCode.InternalServerError.Reason}",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [HttpConstants.ContentTypes.JsonProblem] = new()
                {
                    Schema = context.SchemaGenerator.GenerateSchema(typeof(ProblemDetails),
                        context.SchemaRepository),
                    Examples = new Dictionary<string, OpenApiExample>
                    {
                        {
                            ErrorCode.Unexpected.ToString(), new OpenApiExample
                            {
                                Description = Resources
                                    .DefaultResponsesFilter_Example_UnexpectedError_Description,
                                Summary = Resources.DefaultResponsesFilter_Example_UnexpectedError_Summary,
                                Value = new OpenApiString(Error.Unexpected("A description of the error")
                                    .ToProblem().ProblemDetails.ToJson(true, StringExtensions.JsonCasing.Camel))
                            }
                        }
                    }
                }
            }
        });

        var existingErrorResponses = existingResponses
            .Where(res => res.Key.StartsWith("4") || res.Key.StartsWith("5"));
        foreach (var existingResponse in existingErrorResponses)
        {
            if (generalErrors.TryGetValue(existingResponse.Key, out var error))
            {
                error.Description = existingResponse.Value.Description;
            }
            else
            {
                generalErrors.Add(existingResponse.Key, new OpenApiResponse
                {
                    Description = existingResponse.Value.Description
                });
            }
        }

        var responses = new OpenApiResponses();
        defaultResponse.ForEach(pair => responses.Add(pair.Key, pair.Value));
        validationErrors.ForEach(pair => responses.Add(pair.Key, pair.Value));
        generalErrors.OrderBy(pair => pair.Key).ToList().ForEach(pair => responses.Add(pair.Key, pair.Value));

        return responses;
    }

    /// <summary>
    ///     Returns the description for the default response.
    ///     This may have already been defined by XML Comments, or we just go with the default response code.
    /// </summary>
    private static string GetDefaultResponseDescription(OpenApiResponses existingResponses,
        StatusCode defaultResponseCode)
    {
        var declaredResponse = existingResponses
            .FirstOrDefault(pair => pair.Key.StartsWith("20"));
        if (declaredResponse.Key.Exists())
        {
            return $"{defaultResponseCode.Title}: {declaredResponse.Value.Description}";
        }

        return $"{defaultResponseCode.Title}";
    }

    private static Dictionary<string, OpenApiMediaType>? ConvertResponseTypeToContent(OperationFilterContext context,
        Type responseType)
    {
        if (responseType == typeof(void))
        {
            return null;
        }

        if (responseType == typeof(Stream))
        {
            return new Dictionary<string, OpenApiMediaType>
            {
                [HttpConstants.ContentTypes.OctetStream] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                },
                [HttpConstants.ContentTypes.ImageGif] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                },
                [HttpConstants.ContentTypes.ImageJpeg] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                },
                [HttpConstants.ContentTypes.ImagePng] = new()
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                }
            };
        }

        return new Dictionary<string, OpenApiMediaType>
        {
            [HttpConstants.ContentTypes.Json] = new()
            {
                Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository)
            },
            [HttpConstants.ContentTypes.Xml] = new()
            {
                Schema = context.SchemaGenerator.GenerateSchema(responseType, context.SchemaRepository)
            }
        };
    }

    private static Type ConvertRequestTypeToResponseType(Type requestType)
    {
        if (TryGetResponseType(requestType, out var responseType))
        {
            return responseType;
        }

        if (typeof(IWebRequestStream).IsAssignableFrom(requestType))
        {
            return typeof(Stream);
        }

        return typeof(void);
    }

    private static bool TryGetResponseType(Type requestType, out Type responseType)
    {
        responseType = default!;
        var interfaces = requestType.GetInterfaces();
        var typedRequest = interfaces.FirstOrDefault(@interface =>
            @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IWebRequest<>));
        if (typedRequest.NotExists())
        {
            return false;
        }

        responseType = typedRequest.GetGenericArguments().First();
        return true;
    }

    private static Optional<Type> GetRequestType(OperationFilterContext context)
    {
        var requestParameters = context.MethodInfo.GetParameters()
            .Where(IsWebRequest)
            .ToList();
        if (requestParameters.HasNone())
        {
            return Optional<Type>.None;
        }

        return requestParameters.First().ParameterType;

        static bool IsWebRequest(ParameterInfo requestParameter)
        {
            var type = requestParameter.ParameterType;
            if (type.NotExists())
            {
                return false;
            }

            return typeof(IWebRequest).IsAssignableFrom(type);
        }
    }
}