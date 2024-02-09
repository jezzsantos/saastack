namespace Infrastructure.Web.Api.Common;

/// <summary>
///     Common HTTP MimeTypes
/// </summary>
public static class HttpContentTypes
{
    public const string FormData = "multipart/form-data";
    public const string FormUrlEncoded = "application/x-www-form-urlencoded";
    public const string FormUrlEncodedWithCharset = "application/x-www-form-urlencoded; charset=utf-8";
    public const string Json = "application/json";
    public const string JsonProblem = "application/problem+json";
    public const string JsonWithCharset = "application/json; charset=utf-8";
    public const string OctetStream = "application/octet-stream";
    public const string Xml = "application/xml";
    public const string XmlProblem = "application/problem+xml";
    public const string XmlWithCharset = "application/xml; charset=utf-8";
    public const string Html = "text/html";
}

/// <summary>
///     Common HTTP headers
/// </summary>
public static class HttpHeaders
{
    public const string Accept = "Accept";
    public const string Authorization = "Authorization";
    public const string ContentType = "Content-Type";
    public const string HMACSignature = "X-Hub-Signature";
    public const string RequestId = "Request-ID";
    public const string AntiCSRF = "anti-csrf-tok";
    public const string Origin = "Origin";
    public const string Referer = "Referer";
    public const string SetCookie = "Set-Cookie";
}

/// <summary>
///     Known query parameters
/// </summary>
public static class HttpQueryParams
{
    public const string APIKey = "apikey";
    public const string Format = "format";
}

/// <summary>
///     Known content negotiation formatters
/// </summary>
public static class HttpContentTypeFormatters
{
    public const string Json = "json";
    public const string Xml = "xml";
}

/// <summary>
///     HTTP responses
/// </summary>
public static class HttpResponses
{
    public static class ProblemDetails
    {
        public static class Extensions
        {
            public const string ExceptionPropertyName = "exception";
            public const string ValidationErrorPropertyName = "errors";
        }
    }
}