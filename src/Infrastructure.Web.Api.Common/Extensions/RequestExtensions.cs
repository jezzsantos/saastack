using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class RequestExtensions
{
    public const string EmptyRequestJson = "{}";
    private const char RouteSegmentDelimiter = '/';

    /// <summary>
    ///     Creates a HMAC signature for the specified <see cref="request" />
    /// </summary>
    public static string CreateHMACSignature(this IWebRequest request, string secret)
    {
        var signer = new HMACSigner(request, secret);

        return signer.Sign();
    }

    /// <summary>
    ///     Extracts the <see cref="RequestInfo" /> from the <see cref="RouteAttribute" /> declared on the
    ///     <see cref="request" />
    /// </summary>
    public static RequestInfo GetRequestInfo(this IWebRequest request)
    {
        var requestType = request.GetType();
        var attribute = TryGetRouteFromAttribute(requestType);
        if (attribute.NotExists())
        {
            var requestTypeName = requestType.Name;
            throw new InvalidOperationException(
                Resources.RequestExtensions_MissingRouteAttribute.Format(requestTypeName, nameof(RouteAttribute)));
        }

        var route = ExpandRouteTemplate(request, attribute);

        return new RequestInfo
        {
            Route = route,
            Method = attribute.Method,
            IsTestingOnly = attribute.IsTestingOnly
        };
    }

    /// <summary>
    ///     Returns the placeholders in the route template with their types
    /// </summary>
    public static Dictionary<string, Type> GetRouteTemplatePlaceholders(this Type requestType)
    {
        var attribute = TryGetRouteFromAttribute(requestType);
        if (attribute.NotExists())
        {
            return new Dictionary<string, Type>();
        }

        var fields = GetRequestFieldsWithTypes(requestType);
        if (fields.HasNone())
        {
            return new Dictionary<string, Type>();
        }

        var routeTemplate = attribute.RouteTemplate;
        var placeholders = GetPlaceholders(routeTemplate);
        if (placeholders.HasNone())
        {
            return new Dictionary<string, Type>();
        }

        return fields
            .Where(field => placeholders.Any(ph => ph.Key.EqualsIgnoreCase(field.Key)))
            .ToDictionary(field => field.Key, field => field.Value);
    }

    /// <summary>
    ///     Returns the JSON representation of the specified <see cref="request" />
    /// </summary>
    public static string SerializeToJson(this IWebRequest? request)
    {
        if (request.NotExists())
        {
            return EmptyRequestJson;
        }

        return request.ToJson()!;
    }

    /// <summary>
    ///     Returns the <see cref="RequestInfo.Route" /> for the <see cref="request" />
    /// </summary>
    public static string ToUrl(this IWebRequest request)
    {
        return request.GetRequestInfo().Route;
    }

    private static RouteAttribute? TryGetRouteFromAttribute(Type requestType)
    {
        var attribute = requestType.GetCustomAttribute<RouteAttribute>();
        return attribute.NotExists()
            ? null
            : attribute;
    }

    private static string ExpandRouteTemplate(IWebRequest request, RouteAttribute attribute)
    {
        var routeTemplate = attribute.RouteTemplate;
        var requestFields = GetRequestFieldsWithValues(request);
        if (requestFields.HasNone())
        {
            return routeTemplate;
        }

        var placeholders = GetPlaceholders(routeTemplate);
        if (placeholders.HasNone())
        {
            return PopulateQueryString(attribute, requestFields, new StringBuilder(routeTemplate)).ToString();
        }

        var route = new StringBuilder();
        var positionInOriginalRoute = 0;
        var unSubstitutedRequestFields = new Dictionary<string, object?>(requestFields);
        foreach (var placeholder in placeholders)
        {
            var placeholderStartsAt = placeholder.Value.Index;
            var placeholderLength = placeholder.Value.Length;
            var placeholderEndsAt = placeholderStartsAt + placeholderLength;
            var placeholderText = routeTemplate.Substring(placeholderStartsAt, placeholderLength);

            AppendRouteBeforePlaceholder(routeTemplate, positionInOriginalRoute, placeholderStartsAt, route);

            var requestFieldName = placeholder.Key.ToLowerInvariant();
            if (requestFields.TryGetValue(requestFieldName, out var substitute))
            {
                unSubstitutedRequestFields.Remove(requestFieldName);
                if (substitute.Exists() && substitute.ToString().HasValue())
                {
                    route.Append(substitute);
                }
            }
            else
            {
                route.Append(placeholderText);
            }

            positionInOriginalRoute = placeholderEndsAt;
        }

        AppendRemainingRoute(routeTemplate, positionInOriginalRoute, route);

        return PopulateQueryString(attribute, unSubstitutedRequestFields, route).ToString();

        static void AppendRouteBeforePlaceholder(string routeTemplate, int positionInOriginalRoute,
            int placeholderStartsAt, StringBuilder route)
        {
            var leftOfPlaceholder =
                routeTemplate.Substring(positionInOriginalRoute, placeholderStartsAt - positionInOriginalRoute);
            PruneEmptySegments(route, leftOfPlaceholder);
            route.Append(leftOfPlaceholder);
        }

        static void AppendRemainingRoute(string routeTemplate, int positionInOriginalRoute, StringBuilder route)
        {
            var rightOfPlaceholders = routeTemplate.Substring(positionInOriginalRoute);
            PruneEmptySegments(route, rightOfPlaceholders);
            route.Append(rightOfPlaceholders);
        }

        static void PruneEmptySegments(StringBuilder route, string append)
        {
            if (!append.StartsWith(RouteSegmentDelimiter))
            {
                return;
            }

            if (route.Length > 0 && route[^1] == RouteSegmentDelimiter)
            {
                route.Remove(route.Length - 1, 1);
            }
        }
    }

    private static StringBuilder PopulateQueryString(RouteAttribute attribute,
        Dictionary<string, object?> requestFields,
        StringBuilder route)
    {
        if (attribute.Method is not OperationMethod.Get and not OperationMethod.Search)
        {
            return route;
        }

        var count = 0;
        foreach (var requestField in requestFields)
        {
            var value = requestField.Value;
            if (value.NotExists())
            {
                continue;
            }

            var stringValue = GetStringValue(value);
            if (stringValue.HasValue())
            {
                route.Append(count == 0
                    ? '?'
                    : '&');

                route.Append($"{requestField.Key}={HttpUtility.UrlEncode(stringValue)}");
                count++;
            }
        }

        return route;
    }

    private static string? GetStringValue(object value)
    {
        return value switch
        {
            DateTime dateTimeValue => dateTimeValue.ToIso8601(),
            string stringValue => stringValue,
            _ => value.ToString()
        };
    }

    private static Dictionary<string, (int Index, int Length)> GetPlaceholders(string routeTemplate)
    {
        var matches = Regex.Matches(routeTemplate, @"\{(?<name>[\w\d_]*)\}", RegexOptions.None,
            TimeSpan.FromSeconds(5));
        return matches.ToDictionary(match => match.Groups["name"].Value,
            match => (match.Groups[0].Index, match.Groups[0].Length));
    }

    /// <summary>
    ///     We need to build a dictionary of all public properties, and their values (even if they are null or default),
    ///     where the key is always lowercase (for matching)
    /// </summary>
    private static Dictionary<string, object?> GetRequestFieldsWithValues(IWebRequest request)
    {
        return request.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary
            (
                GetPropertyName,
                propInfo => propInfo.GetValue(request, null)
            );

        static string GetPropertyName(PropertyInfo propInfo)
        {
            var jsonPropertyName = propInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            return jsonPropertyName.Exists()
                ? jsonPropertyName.Name
                : propInfo.Name.ToLowerInvariant();
        }
    }

    /// <summary>
    ///     We need to build a dictionary of all public properties, and their types.
    /// </summary>
    private static Dictionary<string, Type> GetRequestFieldsWithTypes(Type requestType)
    {
        return requestType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.PropertyType
            );
    }
}