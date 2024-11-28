using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using Common.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Interfaces;

/// <inheritdoc />
public abstract partial class WebRequest<TRequest>
{
    /// <summary>
    ///     Provides custom binding that populates the request DTO from the request query, route values,
    ///     and body for <see cref="IsMultiPartFormData" /> requests
    /// </summary>
    public static async ValueTask<TRequest?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var request = context.Request;
        if (IsMultiPartFormData())
        {
            var requestDto = Activator.CreateInstance<TRequest>();
            PopulateFromQueryStringValues(request, requestDto);
            PopulateFromRouteValues(request, requestDto);
            PopulateFromFormValues(request, requestDto);

            return requestDto;
        }

        if (IsFormUrlEncoded())
        {
            var requestDto = Activator.CreateInstance<TRequest>();
            PopulateFromQueryStringValues(request, requestDto);
            PopulateFromRouteValues(request, requestDto);
            PopulateFromFormValues(request, requestDto);

            return requestDto;
        }

        var jsonRequest = await CreateFromJson(context);
        PopulateFromQueryStringValues(request, jsonRequest);
        PopulateFromRouteValues(request, jsonRequest);
        return jsonRequest;
    }

    /// <summary>
    ///     Returns the request DTO, populated from the JSON of the request, or an empty instance if the request has no JSON
    ///     content
    /// </summary>
    private static async Task<TRequest?> CreateFromJson(HttpContext context)
    {
        var jsonOptions = context.RequestServices.GetRequiredService<JsonSerializerOptions>();
        var request = context.Request;
        if (request.HasJsonContentType())
        {
            return await request.ReadFromJsonAsync<TRequest>(jsonOptions);
        }

        return JsonSerializer.Deserialize<TRequest>("{}", jsonOptions);
    }

    /// <summary>
    ///     Populate properties of the specified <see cref="requestDto" /> from any form values
    /// </summary>
    private static void PopulateFromFormValues(HttpRequest request, TRequest requestDto)
    {
        foreach (var (key, stringValues) in request.Form)
        {
            var prop = typeof(TRequest).GetProperty(key,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop.Exists())
            {
                var rawValue = stringValues.Count > 1
                    ? stringValues.ToArray() as object
                    : stringValues.ToString();
                if (rawValue.Exists())
                {
                    var value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFrom(rawValue);
                    prop.SetValue(requestDto, value);
                }
            }
        }
    }

    /// <summary>
    ///     Populate properties of the specified <see cref="requestDto" /> from any route values
    /// </summary>
    private static void PopulateFromRouteValues(HttpRequest request, TRequest? requestDto)
    {
        foreach (var (key, rawValue) in request.RouteValues)
        {
            var prop = typeof(TRequest).GetProperty(key,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop.Exists())
            {
                if (rawValue.Exists())
                {
                    var value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFrom(rawValue);
                    prop.SetValue(requestDto, value);
                }
            }
        }
    }

    /// <summary>
    ///     Populate properties of the specified <see cref="requestDto" /> from any query string values
    /// </summary>
    private static void PopulateFromQueryStringValues(HttpRequest request, TRequest? requestDto)
    {
        foreach (var (key, stringValues) in request.Query)
        {
            var prop = typeof(TRequest).GetProperty(key,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop.Exists())
            {
                var rawValue = stringValues.Count > 1
                    ? stringValues.ToArray() as object
                    : stringValues.ToString();
                if (rawValue.Exists())
                {
                    var value = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFrom(rawValue);
                    prop.SetValue(requestDto, value);
                }
            }
        }
    }

    private static bool IsMultiPartFormData()
    {
        return typeof(TRequest).IsAssignableTo(typeof(IHasMultipartFormData));
    }

    private static bool IsFormUrlEncoded()
    {
        return typeof(TRequest).IsAssignableTo(typeof(IHasFormUrlEncoded));
    }
}