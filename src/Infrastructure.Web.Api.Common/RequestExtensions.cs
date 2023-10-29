using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common;

public static class RequestExtensions
{
    private const char RouteSegmentDelimiter = '/';

    /// <summary>
    ///     Extracts the <see cref="RequestInfo" /> from the <see cref="RouteAttribute" /> declared on the
    ///     <see cref="request" />
    /// </summary>
    public static RequestInfo GetRequestInfo(this IWebRequest request)
    {
        var attribute = GetRouteFromAttribute(request);

        var route = ExpandRouteTemplate(request, attribute);

        return new RequestInfo
        {
            Route = route,
            Operation = attribute.Operation,
            IsTestingOnly = attribute.IsTestingOnly
        };
    }

    /// <summary>
    ///     Rewinds the <see cref="HttpRequest.Body" /> back to the start
    /// </summary>
    public static void RewindBody(this HttpRequest httpRequest)
    {
        if (httpRequest.Body.CanSeek)
        {
            httpRequest.Body.Seek(0, SeekOrigin.Begin);
        }
    }

    private static RouteAttribute GetRouteFromAttribute(IWebRequest request)
    {
        var attribute = request.GetType().GetCustomAttribute<RouteAttribute>();
        if (attribute.NotExists())
        {
            var requestTypeName = request.GetType().Name;
            throw new InvalidOperationException(
                Resources.RequestExtensions_MissingRouteAttribute.Format(requestTypeName, nameof(RouteAttribute)));
        }

        return attribute;
    }

    private static string ExpandRouteTemplate(IWebRequest request, RouteAttribute attribute)
    {
        var routeTemplate = attribute.RouteTemplate;
        var requestFields = GetRequestFields(request);
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
        if (attribute.Operation is not ServiceOperation.Get and not ServiceOperation.Search)
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
    private static Dictionary<string, object?> GetRequestFields(IWebRequest request)
    {
        return request.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ToDictionary
        (
            propInfo => propInfo.Name.ToLowerInvariant(),
            propInfo => propInfo.GetValue(request, null)
        );
    }
}