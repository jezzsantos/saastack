using System.ComponentModel;
using System.Reflection;
using Common.Extensions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Interfaces;

/// <inheritdoc />
public abstract partial class WebRequest<TRequest>
{
    /// <summary>
    ///     Provides custom binding that populates the request DTO from the request query, route values,
    ///     and body for <see cref="IsMultiPartForm" /> requests
    /// </summary>
    public static async ValueTask<TRequest?> BindAsync(HttpContext context, ParameterInfo parameter)
    {
        var request = context.Request;
        if (IsMultiPartForm())
        {
            var requestDto = Activator.CreateInstance<TRequest>();
            PopulateFromQueryStringValues(request, requestDto);
            PopulateFromRouteValues(request, requestDto);
            PopulateFromFormValues(request, requestDto);

            return requestDto;
        }
        else
        {
            var requestDto = await request.ReadFromJsonAsync<TRequest>();
            PopulateFromQueryStringValues(request, requestDto);
            PopulateFromRouteValues(request, requestDto);
            return requestDto;
        }
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

    private static bool IsMultiPartForm()
    {
        return typeof(TRequest).IsAssignableTo(typeof(IHasMultipartForm));
    }
}