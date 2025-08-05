using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Extensions;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Interfaces;

/// <inheritdoc />
public abstract partial class WebRequest<TRequest>
{
    /// <summary>
    ///     Provides custom binding that populates the request DTO instance from data found in either: query string, route
    ///     values, and/or form values for <see cref="IsMultiPartFormData" /> or <see cref="IsFormUrlEncoded" /> requests.
    ///     Body values will be taken care of automatically by ASPNET, since that is the default binding.
    ///     This custom binding eliminates the need for using: [FromQuery], [FromRoute], [FromBody], [FromForm] attributes.
    ///     This method is automatically defined in all typed request types derived from <see cref="WebRequest{TRequest}" />.
    ///     Refer to custom binding in ASPNET:
    ///     <see
    ///         href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/parameter-binding?view=aspnetcore-9.0#custom-binding" />
    /// </summary>
    public static async ValueTask<TRequest?> BindAsync(HttpContext context, ParameterInfo _)
    {
        var request = context.Request;
        var requestProperties = BuildRequestProperties(typeof(TRequest));
        if (IsMultiPartFormData())
        {
            var requestDto = Activator.CreateInstance<TRequest>();
            PopulateFromQueryStringValues(request, requestDto, requestProperties);
            PopulateFromRouteValues(request, requestDto, requestProperties);
            PopulateFromFormValues(request, requestDto, requestProperties);

            return requestDto;
        }

        if (IsFormUrlEncoded())
        {
            var requestDto = Activator.CreateInstance<TRequest>();
            PopulateFromQueryStringValues(request, requestDto, requestProperties);
            PopulateFromRouteValues(request, requestDto, requestProperties);
            PopulateFromFormValues(request, requestDto, requestProperties);

            return requestDto;
        }

        var jsonRequest = await CreateFromJsonAsync(context, context.RequestAborted);
        PopulateFromQueryStringValues(request, jsonRequest, requestProperties);
        PopulateFromRouteValues(request, jsonRequest, requestProperties);
        return jsonRequest;
    }

    /// <summary>
    ///     Returns the request DTO, populated from the JSON of the request, or an empty instance if the request has no JSON
    ///     content
    /// </summary>
    private static async Task<TRequest?> CreateFromJsonAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var jsonOptions = context.RequestServices.GetRequiredService<JsonSerializerOptions>();
        var request = context.Request;
        if (request.HasJsonContentType())
        {
            return await request.ReadFromJsonAsync<TRequest>(jsonOptions, cancellationToken);
        }

        return JsonSerializer.Deserialize<TRequest>(HttpConstants.EmptyRequestJson, jsonOptions);
    }

    /// <summary>
    ///     Populate properties of the specified <see cref="requestDto" /> from any form values
    /// </summary>
    private static void PopulateFromFormValues(HttpRequest request, TRequest requestDto,
        Dictionary<string, PropertyInfo> allProperties)
    {
        if (request.Form.HasNone())
        {
            return;
        }

        if (requestDto.NotExists())
        {
            return;
        }

        if (allProperties.HasNone())
        {
            return;
        }

        foreach (var (key, stringValues) in request.Form)
        {
            if (allProperties.TryGetValue(key, out var prop))
            {
                var rawValue = stringValues.Count > 1
                    ? stringValues.ToArray() as object
                    : stringValues.ToString();
                if (rawValue.Exists())
                {
                    SetPropertyValue(prop, requestDto, rawValue);
                }
            }
        }
    }

    /// <summary>
    ///     Populate properties of the specified <see cref="requestDto" /> from any route values
    /// </summary>
    private static void PopulateFromRouteValues(HttpRequest request, TRequest? requestDto,
        Dictionary<string, PropertyInfo> allProperties)
    {
        if (request.RouteValues.HasNone())
        {
            return;
        }

        if (requestDto.NotExists())
        {
            return;
        }

        if (allProperties.HasNone())
        {
            return;
        }

        foreach (var (key, rawValue) in request.RouteValues)
        {
            if (allProperties.TryGetValue(key, out var prop))
            {
                if (rawValue.Exists())
                {
                    SetPropertyValue(prop, requestDto, rawValue);
                }
            }
        }
    }

    /// <summary>
    ///     Populate properties of the specified <see cref="requestDto" /> from any query string values
    /// </summary>
    private static void PopulateFromQueryStringValues(HttpRequest request, TRequest? requestDto,
        Dictionary<string, PropertyInfo> allProperties)
    {
        if (request.Query.HasNone())
        {
            return;
        }

        if (requestDto.NotExists())
        {
            return;
        }

        if (allProperties.HasNone())
        {
            return;
        }

        foreach (var (key, stringValues) in request.Query)
        {
            var keyName = PruneParameterName(key);
            if (allProperties.TryGetValue(keyName, out var prop))
            {
                var rawValue = stringValues.Count > 1
                    ? stringValues.ToArray() as object
                    : stringValues.ToString();
                if (rawValue.Exists())
                {
                    SetPropertyValue(prop, requestDto, rawValue);
                }
            }
        }

        return;

        static string PruneParameterName(string parameterName)
        {
            //To support axios array syntax
            return parameterName.EndsWith("[]")
                ? parameterName.Substring(0, parameterName.Length - 2)
                : parameterName;
        }
    }

    private static void SetPropertyValue(PropertyInfo prop, TRequest requestDto, object rawValue)
    {
        if (prop.PropertyType == typeof(DateTime))
        {
            var dateTime = rawValue.ToString()!.FromIso8601();
            prop.SetValue(requestDto, dateTime);
            return;
        }

        if (prop.PropertyType == typeof(DateTime?))
        {
            var dateTime = rawValue.ToString()!.FromIso8601();
            if (dateTime == DateTime.MinValue)
            {
                return;
            }

            prop.SetValue(requestDto, dateTime);
            return;
        }

        if (prop.PropertyType == rawValue.GetType())
        {
            prop.SetValue(requestDto, rawValue);
            return;
        }

        var typeConverter = TypeDescriptor.GetConverter(prop.PropertyType);
        var value = typeConverter.ConvertFrom(rawValue);
        prop.SetValue(requestDto, value);
    }

    private static bool IsMultiPartFormData()
    {
        return typeof(TRequest).IsAssignableTo(typeof(IHasMultipartFormData));
    }

    private static bool IsFormUrlEncoded()
    {
        return typeof(TRequest).IsAssignableTo(typeof(IHasFormUrlEncoded));
    }

    /// <summary>
    ///     Adds all properties of the request type to a dictionary, including any properties that are defined with the
    ///     <see cref="JsonPropertyNameAttribute" />
    ///     None: The entries with the name of the <see cref="JsonPropertyNameAttribute" /> have used the
    ///     same <see cref="PropertyInfo" /> as the original property name
    /// </summary>
    private static Dictionary<string, PropertyInfo> BuildRequestProperties(Type requestType)
    {
        var allProperties = new Dictionary<string, PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
        var classProperties = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (classProperties.HasNone())
        {
            return new Dictionary<string, PropertyInfo>();
        }

        foreach (var propertyInfo in classProperties)
        {
            allProperties.Add(propertyInfo.Name, propertyInfo);
            var attribute = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (attribute.Exists() && attribute.Name.HasValue())
            {
                allProperties.TryAdd(attribute.Name, propertyInfo);
            }
        }

        return allProperties;
    }
}