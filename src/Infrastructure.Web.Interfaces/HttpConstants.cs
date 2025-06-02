using System.Net;
using Common;

namespace Infrastructure.Web.Interfaces;

public static class HttpConstants
{
    /// <summary>
    ///     Common HTTP MimeTypes
    /// </summary>
    public static class ContentTypes
    {
        public const string Everything = "*/*";
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string Html = "text/html";
        public const string ImageGif = "image/gif";
        public const string ImageJpeg = "image/jpeg";
        public const string ImageJpegWithCharset = "image/jpeg; utf-8";
        public const string ImagePng = "image/png";
        public const string Json = "application/json";
        public const string JsonProblem = "application/problem+json";
        public const string JsonWithCharset = "application/json; charset=utf-8";
        public const string MultiPartFormData = "multipart/form-data";
        public const string OctetStream = "application/octet-stream";
        public const string Text = "text/plain";
        public const string Xml = "application/xml";
        public const string XmlProblem = "application/problem+xml";
        public const string XmlWithCharset = "application/xml; charset=utf-8";
    }

    /// <summary>
    ///     Common HTTP headers
    /// </summary>
    public static class Headers
    {
        public const string Accept = "Accept";
        public const string AntiCSRF = "anti-csrf-tok";
        public const string Authorization = "Authorization";
        public const string ContentLength = "Content-Length";
        public const string ContentType = "Content-Type";
        public const string HMACSignature = "X-HMAC-Signature";
        public const string PrivateInterHostSignature = "X-InterHost-Signature";
        public const string Origin = "Origin";
        public const string Referer = "Referer";
        public const string RequestId = "Request-Id";
        public const string SetCookie = "Set-Cookie";
        public const string Cookie = "Cookie";
        public const string Tenant = "Tenant";
    }

    /// <summary>
    ///     Known query parameters
    /// </summary>
    public static class QueryParams
    {
        public const string APIKey = "apikey";
        public const string Format = "format";
    }

    /// <summary>
    ///     Known content negotiation formatters
    /// </summary>
    public static class ContentTypeFormatters
    {
        public const string Json = "json";
        public const string Xml = "xml";
    }

    /// <summary>
    ///     HTTP responses
    /// </summary>
    public static class Responses
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

    /// <summary>
    ///     HTTP Status codes and descriptions
    /// </summary>
    public static class StatusCodes
    {
        /// <summary>
        ///     Defines the map of HTTP status codes to <see cref="ErrorCode" />
        /// </summary>
        public static readonly Dictionary<HttpStatusCode, IReadOnlyList<ErrorCode>> SupportedErrorCodesMap = new()
        {
            //EXTEND: other HTTP status codes to error maps
            { HttpStatusCode.BadRequest, new List<ErrorCode> { ErrorCode.Validation, ErrorCode.RuleViolation } },
            { HttpStatusCode.Unauthorized, new List<ErrorCode> { ErrorCode.NotAuthenticated } },
            { HttpStatusCode.PaymentRequired, new List<ErrorCode> { ErrorCode.FeatureViolation } },
            { HttpStatusCode.Forbidden, new List<ErrorCode> { ErrorCode.RoleViolation, ErrorCode.ForbiddenAccess } },
            { HttpStatusCode.NotFound, new List<ErrorCode> { ErrorCode.EntityNotFound } },
            {
                HttpStatusCode.MethodNotAllowed,
                new List<ErrorCode> { ErrorCode.PreconditionViolation, ErrorCode.EntityDeleted }
            },
            { HttpStatusCode.Conflict, new List<ErrorCode> { ErrorCode.EntityExists } },
            { HttpStatusCode.Locked, new List<ErrorCode> { ErrorCode.EntityLocked } },
            { HttpStatusCode.InternalServerError, new List<ErrorCode> { ErrorCode.Unexpected } }
        };

        /// <summary>
        ///     Defines the supported errors
        /// </summary>
        public static readonly Dictionary<HttpStatusCode, StatusCode> SupportedErrorStatuses = new[]
        {
            //EXTEND: other supported error status codes
            StatusCode.BadRequest,
            StatusCode.Unauthorized,
            StatusCode.PaymentRequired,
            StatusCode.Forbidden,
            StatusCode.NotFound,
            StatusCode.MethodNotAllowed,
            StatusCode.Conflict,
            StatusCode.Locked
        }.ToDictionary(pair => pair.Code, pair => pair);
    }
}