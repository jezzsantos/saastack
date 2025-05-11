using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Web;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IO;
using RouteAttribute = Infrastructure.Web.Api.Interfaces.RouteAttribute;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class RequestExtensions
{
    public const string EmptyRequestJson = "{}";
    private const char RouteSegmentDelimiter = '/';
    private static readonly RecyclableMemoryStreamManager MemoryManager = new();

    /// <summary>
    ///     Extracts the <see cref="RequestInfo" /> from the <see cref="Interfaces.RouteAttribute" /> declared on the
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

        var (route, routeParams) = ExpandRouteTemplate(request, attribute);

        return new RequestInfo
        {
            Route = route,
            Method = attribute.Method,
            IsTestingOnly = attribute.IsTestingOnly,
            RouteParams = routeParams
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
    ///     Extracts the <see cref="RequestInfo" /> from the <see cref="RouteAttribute" /> declared on the
    ///     <see cref="request" />, and removes any fields from the body of the request.
    /// </summary>
    public static (RequestInfo Info, IWebRequest Request) ParseRequestInfo(this IWebRequest request)
    {
        var info = GetRequestInfo(request);
        var adjustedRequest = NullifyRequestFields(info.RouteParams, request);

        return (info, adjustedRequest);
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
    ///     Sets the HMAC signature header on the specified <see cref="message" /> by signing the body of the specified
    ///     <see cref="request" />
    /// </summary>
    public static void SetHMACAuth(this HttpRequestMessage message, IWebRequest request, string secret)
    {
        var signature = request.CreateHMACSignature(secret);

        message.Headers.Add(HttpConstants.Headers.HMACSignature, signature);
    }

    /// <summary>
    ///     Sets the HMAC signature header on the specified <see cref="message" /> by signing the body of the specified
    ///     <see cref="request" />
    /// </summary>
    public static void SetPrivateInterHostAuth(this HttpRequestMessage message, IWebRequest request, string secret,
        string? token = null)
    {
        var signature = request.CreateHMACSignature(secret);

        message.Headers.Add(HttpConstants.Headers.PrivateInterHostSignature, signature);
        if (token.HasValue())
        {
            message.SetJWTBearerToken(token);
        }
    }

    /// <summary>
    ///     Sets the HMAC signature header on the specified <see cref="message" /> by signing the body of the specified
    ///     <see cref="message" />
    /// </summary>
    public static void SetPrivateInterHostAuth(this HttpRequestMessage message, string secret,
        string? token = null)
    {
        var signature = message.CreateHMACSignature(secret);

        message.Headers.Add(HttpConstants.Headers.PrivateInterHostSignature, signature);
        if (token.HasValue())
        {
            message.SetJWTBearerToken(token);
        }
    }

    /// <summary>
    ///     Returns the <see cref="RequestInfo.Route" /> for the <see cref="request" />
    /// </summary>
    public static string ToUrl(this IWebRequest request)
    {
        return request.GetRequestInfo().Route;
    }

    private static string SerializeToJson(this Dictionary<string, object?> fields)
    {
        if (fields.HasNone())
        {
            return EmptyRequestJson;
        }

        return fields.ToJson()!;
    }

    /// <summary>
    ///     Creates a HMAC signature for the specified <see cref="request" />.
    ///     Note: We define the data to be hashed with HMAC as: {body}
    ///     Where {body} for POST, PUTPATCH requests will be the JSON of its fields, except for body fields in the path,
    ///     or marked with [FromQuery], [FromRoute] or [JsonIgnore]
    ///     Where {body} for GET, SEARCH, DELETE requests will always be the characters <see cref="EmptyRequestJson" />
    /// </summary>
    private static string CreateHMACSignature(this IWebRequest request, string secret)
    {
        var body = request.CanHaveBody()
            ? GetSerializedBody(request)
            : EmptyRequestJson;

        var data = HMACSigner.SignatureEncoding.GetBytes(body);
        var signer = new HMACSigner(data, secret);

        return signer.Sign();

        static string GetSerializedBody(IWebRequest webRequest)
        {
            var fields = GetRequestFieldsWithValues(webRequest, false);
            var fromRouteFields = GetRouteFields(webRequest);
            foreach (var fromRouteField in fromRouteFields)
            {
                if (fields.TryGetValue(fromRouteField, out var field))
                {
                    field.Binding = RequestFieldBinding.FromRoute;
                }
            }

            var bodyFields = fields
                .Where(pair => pair.Value.Binding == RequestFieldBinding.Default)
                .Where(pair => pair.Value.Value is not null)
                .ToDictionary(field => field.Key, field => field.Value.Value);
            return bodyFields.SerializeToJson();
        }

        static List<string> GetRouteFields(IWebRequest webRequest)
        {
            var attribute = TryGetRouteFromAttribute(webRequest.GetType());
            if (attribute.NotExists())
            {
                return [];
            }

            var placeholders = GetPlaceholders(attribute.RouteTemplate);
            return placeholders
                .Select(pair => pair.Key)
                .ToList();
        }
    }

    /// <summary>
    ///     Creates a HMAC signature for the specified <see cref="message" />.
    ///     Note: We define the data to be hashed with HMAC as: {body}
    ///     Where {body} for POST, PUTPATCH requests will be the JSON of its fields, except for body fields in the path,
    ///     or marked with [FromQuery], [FromRoute] or [JsonIgnore]
    ///     Where {body} for GET, SEARCH, DELETE requests will always be the characters <see cref="EmptyRequestJson" />
    /// </summary>
    private static string CreateHMACSignature(this HttpRequestMessage message, string secret)
    {
        var bytes = message.Method.CanHaveBody()
            ? GetSerializedBody(message)
            : [..HMACSigner.SignatureEncoding.GetBytes(EmptyRequestJson)];

        var data = bytes.ToArray();
        var signer = new HMACSigner(data, secret);

        return signer.Sign();

        static List<byte> GetSerializedBody(HttpRequestMessage message)
        {
            var emptyBody = new List<byte>(HMACSigner.SignatureEncoding.GetBytes(EmptyRequestJson));
            if (message.Content.NotExists())
            {
                return emptyBody;
            }

            using var stream = MemoryManager.GetStream("HMACSigner");
            message.Content.CopyTo(stream, null, CancellationToken.None);
            if (stream.Length <= 0)
            {
                return emptyBody;
            }

            stream.Seek(0, SeekOrigin.Begin);
            return [..stream.ToArray()];
        }
    }

    private static bool CanHaveBody(this IWebRequest request)
    {
        var attribute = request.GetType().GetCustomAttribute<RouteAttribute>();
        if (attribute.NotExists())
        {
            return false;
        }

        var method = attribute.Method;
        return method.CanHaveBody();
    }

    private static IWebRequest NullifyRequestFields(Dictionary<string, object?> routeParams,
        IWebRequest request)
    {
        var properties = request.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.HasNone())
        {
            return request;
        }

        if (routeParams.HasNone())
        {
            return request;
        }

        foreach (var routeParam in routeParams)
        {
            var property = properties.FirstOrDefault(prop => prop.Name.EqualsIgnoreCase(routeParam.Key));
            if (property.Exists())
            {
                property.SetValue(request, null);
            }
        }

        return request;
    }

    private static RouteAttribute? TryGetRouteFromAttribute(Type requestType)
    {
        var attribute = requestType.GetCustomAttribute<RouteAttribute>();
        return attribute.NotExists()
            ? null
            : attribute;
    }

    private static (string Route, Dictionary<string, object?> RouteParams) ExpandRouteTemplate(IWebRequest request,
        RouteAttribute attribute)
    {
        var routeTemplate = attribute.RouteTemplate;
        var requestFields = GetRequestFieldsWithValues(request);
        if (requestFields.HasNone())
        {
            return (routeTemplate,
                new Dictionary<string, object?>());
        }

        var routeSegments = new List<string>();
        if (routeTemplate.Contains("#"))
        {
            var startIndex = routeTemplate.IndexOf("#", StringComparison.Ordinal);
            var bookmarkSegment = routeTemplate.Substring(startIndex);
            routeSegments.Add(bookmarkSegment);
            routeTemplate = routeTemplate.Substring(0, startIndex);
        }

        var placeholders = GetPlaceholders(routeTemplate);
        if (placeholders.HasNone())
        {
            routeSegments.Insert(0, routeTemplate);
            PopulateQueryString(attribute, requestFields, ref routeSegments);

            return (routeSegments.Join(string.Empty), new Dictionary<string, object?>());
        }

        var positionInOriginalRoute = 0;
        var unSubstitutedRequestFields = new Dictionary<string, RequestFieldDescriptor>(requestFields);
        var substitutedRequestFields = new Dictionary<string, object?>();
        foreach (var placeholder in placeholders)
        {
            var placeholderStartsAt = placeholder.Value.Index;
            var placeholderLength = placeholder.Value.Length;
            var placeholderEndsAt = placeholderStartsAt + placeholderLength;
            var placeholderText = routeTemplate.Substring(placeholderStartsAt, placeholderLength);

            AppendRouteBeforePlaceholder(routeTemplate, positionInOriginalRoute, placeholderStartsAt, routeSegments);

            var requestFieldName = placeholder.Key.ToLowerInvariant();
            if (requestFields.TryGetValue(requestFieldName, out var substitute))
            {
                unSubstitutedRequestFields.Remove(requestFieldName);
                if (substitute.Value.Exists() && substitute.Value.ToString().HasValue())
                {
                    substitutedRequestFields.Add(requestFieldName, substitute.Value);
                    var segment = substitute.Value.ToString();
                    if (segment.HasValue())
                    {
                        routeSegments.Add(segment);
                    }
                }
            }
            else
            {
                routeSegments.Add(placeholderText);
            }

            positionInOriginalRoute = placeholderEndsAt;
        }

        AppendRemainingRoute(routeTemplate, positionInOriginalRoute, routeSegments);
        PopulateQueryString(attribute, unSubstitutedRequestFields, ref routeSegments);

        return (routeSegments.Join(string.Empty), substitutedRequestFields);

        static void AppendRouteBeforePlaceholder(string routeTemplate, int positionInOriginalRoute,
            int placeholderStartsAt, List<string> routeSegments)
        {
            var leftOfPlaceholder =
                routeTemplate.Substring(positionInOriginalRoute, placeholderStartsAt - positionInOriginalRoute);
            PruneEmptySegments(routeSegments, leftOfPlaceholder);
            routeSegments.Add(leftOfPlaceholder);
        }

        static void AppendRemainingRoute(string routeTemplate, int positionInOriginalRoute, List<string> routeSegments)
        {
            var rightOfPlaceholders = routeTemplate.Substring(positionInOriginalRoute);
            PruneEmptySegments(routeSegments, rightOfPlaceholders);
            if (rightOfPlaceholders.HasValue())
            {
                routeSegments.Add(rightOfPlaceholders);
            }
        }

        static void PruneEmptySegments(List<string> routeSegments, string append)
        {
            if (!append.StartsWith(RouteSegmentDelimiter))
            {
                return;
            }

            if (routeSegments.Count > 0
                && routeSegments[^1].EndsWith(RouteSegmentDelimiter))
            {
                routeSegments[^1] = routeSegments[^1].TrimEnd(RouteSegmentDelimiter);
            }
        }
    }

    private static void PopulateQueryString(RouteAttribute attribute,
        Dictionary<string, RequestFieldDescriptor> requestFields, ref List<string> routeSegments)
    {
        if (requestFields.HasNone())
        {
            return;
        }

        var requiresFromQueryDeclaration = attribute.Method is not OperationMethod.Get and not OperationMethod.Search;
        var addedFieldCount = 0;
        foreach (var requestField in requestFields)
        {
            var fieldValue = requestField.Value.Value;
            if (fieldValue.NotExists())
            {
                continue;
            }

            if (requiresFromQueryDeclaration
                && requestField.Value.Binding != RequestFieldBinding.FromQuery)
            {
                continue;
            }

            var segment = new StringBuilder();
            segment.Append(addedFieldCount == 0
                ? '?'
                : '&');

            var values = GetValuePairs(requestField);
            var valueCount = 0;
            foreach (var value in values)
            {
                if (valueCount > 0)
                {
                    segment.Append('&');
                }

                segment.Append(value);
                valueCount++;
            }

            routeSegments.Add(segment.ToString());
            addedFieldCount++;
        }

        var bookmarkSegment = routeSegments
            .FirstOrDefault(segment => segment.StartsWith("#"));
        var hasBookmark = bookmarkSegment.Exists();
        if (hasBookmark
            && addedFieldCount > 0)
        {
            var oldIndex = routeSegments.IndexOf(bookmarkSegment!);
            routeSegments.RemoveAt(oldIndex);
            routeSegments.Add(bookmarkSegment!);
        }
    }

    private static List<string> GetValuePairs(KeyValuePair<string, RequestFieldDescriptor> requestField)
    {
        var fieldValue = requestField.Value.Value;
        if (fieldValue.NotExists())
        {
            return [];
        }

        if (requestField.Value.Type.IsAssignableTo(typeof(Array)))
        {
            var enumerable = fieldValue as Array;
            return enumerable!.Cast<object>()
                .Select(CreatePair)
                .ToList();
        }

        return [CreatePair(fieldValue)];

        string CreatePair(object value)
        {
            return $"{requestField.Key}={HttpUtility.UrlEncode(GetStringValue(value))}";
        }
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
    private static Dictionary<string, RequestFieldDescriptor> GetRequestFieldsWithValues(IWebRequest request,
        bool lowercaseNames = true)
    {
        return request.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary
            (
                propInfo => GetPropertyName(propInfo, lowercaseNames),
                propInfo =>
                {
                    var binding = RequestFieldBinding.Default;
                    var fromQuery = propInfo.GetCustomAttribute<FromQueryAttribute>();
                    if (fromQuery.Exists())
                    {
                        binding = RequestFieldBinding.FromQuery;
                    }
                    else
                    {
                        var fromRoute = propInfo.GetCustomAttribute<FromRouteAttribute>();
                        if (fromRoute.Exists())
                        {
                            binding = RequestFieldBinding.FromRoute;
                        }
                        else
                        {
                            var ignore = propInfo.GetCustomAttribute<JsonIgnoreAttribute>();
                            if (ignore.Exists())
                            {
                                binding = RequestFieldBinding.Ignore;
                            }
                        }
                    }

                    return new RequestFieldDescriptor(propInfo.PropertyType, propInfo.GetValue(request, null),
                        binding);
                });

        static string GetPropertyName(PropertyInfo propInfo, bool lowercaseNames)
        {
            var jsonPropertyName = propInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
            return jsonPropertyName.Exists()
                ? jsonPropertyName.Name
                : lowercaseNames
                    ? propInfo.Name.ToLowerInvariant()
                    : propInfo.Name;
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

    private record RequestFieldDescriptor(Type Type, object? Value, RequestFieldBinding Binding)
    {
        public RequestFieldBinding Binding { get; set; } = Binding;
    }

    private enum RequestFieldBinding
    {
        Default = 0, //Noting defined
        Ignore = 1, //Explicitly marked as [JsonIgnore]
        FromQuery = 2, //Explicitly marked as [FromQuery]
        FromRoute = 3 //Explicitly marked as [FromRoute]
    }
}