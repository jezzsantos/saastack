using System.Net.Http.Headers;
using System.Text;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class HttpRequestExtensions
{
    private const string BearerTokenPrefix = "Bearer";

    /// <summary>
    ///     Whether the specified <see cref="method" /> could have a content body.
    /// </summary>
    public static bool CanHaveBody(this HttpMethod method)
    {
        return method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch;
    }

    /// <summary>
    ///     Returns the value of the APIKEY authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetAPIKeyAuth(this HttpRequest request)
    {
        var fromQuery = request.Query[HttpConstants.QueryParams.APIKey].FirstOrDefault();
        if (fromQuery.HasValue())
        {
            return fromQuery;
        }

        var fromBasicAuth = GetBasicAuth(request);
        if (!fromBasicAuth.Username.HasValue)
        {
            return Optional<string>.None;
        }

        if (fromBasicAuth.Username.HasValue
            && !fromBasicAuth.Password.HasValue)
        {
            return fromBasicAuth.Username;
        }

        return Optional<string>.None;
    }

    /// <summary>
    ///     Returns the values of the BASIC authentication from the request (if any)
    /// </summary>
    public static (Optional<string> Username, Optional<string> Password) GetBasicAuth(this HttpRequest request)
    {
        var fromBasicAuth = AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var result)
            ? result
            : null;
        if (fromBasicAuth.NotExists())
        {
            return (Optional<string>.None, Optional<string>.None);
        }

        var token = result!.Parameter;
        if (token.HasNoValue())
        {
            return (Optional<string>.None, Optional<string>.None);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var delimiterIndex = decoded.IndexOf(':', StringComparison.Ordinal);
            var username = decoded.Substring(0, delimiterIndex);
            var password = decoded.Substring(delimiterIndex + 1);
            return (username.HasValue()
                    ? username
                    : Optional<string>.None,
                password.HasValue()
                    ? password
                    : Optional<string>.None);
        }
        catch (FormatException)
        {
            return (Optional<string>.None, Optional<string>.None);
        }
        catch (IndexOutOfRangeException)
        {
            return (Optional<string>.None, Optional<string>.None);
        }
    }

    /// <summary>
    ///     Returns the value of the HMAC signature authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetHMACAuth(this HttpRequest request)
    {
        var authorization = request.Headers[HttpConstants.Headers.HMACSignature];
        if (authorization.NotExists() || authorization.Count == 0)
        {
            return Optional<string>.None;
        }

        var signature = authorization.FirstOrDefault();
        if (signature.HasNoValue())
        {
            return Optional<string>.None;
        }

        return signature;
    }

    /// <summary>
    ///     Returns the value of the Bearer token of the JWT authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetTokenAuth(this HttpRequest request)
    {
        var authorization = request.Headers.Authorization;
        if (authorization.NotExists() || authorization.Count == 0)
        {
            return Optional<string>.None;
        }

        var value = authorization.FirstOrDefault(val => val.HasValue() && val.StartsWith(BearerTokenPrefix));
        if (value.HasNoValue())
        {
            return Optional<string>.None;
        }

        var indexOfToken = BearerTokenPrefix.Length + 1;
        var token = value.Substring(indexOfToken);

        return token.HasValue()
            ? token
            : Optional<string>.None;
    }

    /// <summary>
    ///     Returns the uploaded file from the specified <see cref="request" />,
    ///     given a specified <see cref="maxSizeInBytes" /> and <see cref="allowableContentTypes" />
    /// </summary>
    public static Result<FileUpload, Error> GetUploadedFile(this HttpRequest request,
        IFileUploadService fileUploadService, long maxSizeInBytes, IReadOnlyList<string> allowableContentTypes)
    {
        var uploads = request.Form.Files
            .Select(file => new FileUpload
            {
                Content = file.OpenReadStream(),
                ContentType = FileUploadContentType.FromContentType(file.ContentType),
                Filename = file.FileName,
                Size = file.Length
            }).ToList();

        return fileUploadService.GetUploadedFile(uploads, maxSizeInBytes, allowableContentTypes);
    }

    /// <summary>
    ///     Whether the MediaType (of the ContentType) is the specified <see cref="contentType" />
    /// </summary>
    public static bool IsContentType(this HttpRequest request, string contentType)
    {
        return contentType.IsMediaType(request.ContentType);
    }

    /// <summary>
    ///     Rewinds the <see cref="HttpRequest.Body" /> back to the start
    /// </summary>
    public static void RewindBody(this HttpRequest httpRequest)
    {
        if (httpRequest.Body.CanSeek)
        {
            httpRequest.Body.Rewind();
        }
    }

    /// <summary>
    ///     Sets the <see cref="HttpConstants.Headers.Authorization" /> header of the specified <see cref="message" />
    ///     to the <see cref="apiKey" />
    /// </summary>
    public static void SetAPIKey(this HttpRequestMessage message, string apiKey)
    {
        if (apiKey.HasNoValue())
        {
            return;
        }

        message.SetBasicAuth(apiKey);
    }

    /// <summary>
    ///     Sets the <see cref="ICallerContext.Authorization" /> to the specified <see cref="message" />
    /// </summary>
    public static void SetAuthorization(this HttpRequestMessage message, ICallerContext caller)
    {
        var authorization = caller.Authorization;
        if (!authorization.HasValue)
        {
            return;
        }

        switch (authorization.Value.Method)
        {
            case ICallerContext.AuthorizationMethod.Token:
                SetJWTBearerToken(message, authorization.Value.Value);
                break;

            case ICallerContext.AuthorizationMethod.APIKey:
                SetAPIKey(message, authorization.Value.Value);
                break;

            case ICallerContext.AuthorizationMethod.HMAC:
                throw new InvalidOperationException(Resources.HttpRequestExtensions_HMACAuthorizationNotSupported);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    ///     Sets the <see cref="ICallerContext.Authorization" /> to Basic with <see cref="username" />, and
    ///     <see cref="password" />
    /// </summary>
    public static void SetBasicAuth(this HttpRequestMessage message, string username, string? password = null)
    {
        if (username.HasNoValue())
        {
            return;
        }

        message.Headers.Add(HttpConstants.Headers.Authorization,
            $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{(password.HasValue() ? password : string.Empty)}"))}");
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
    ///     Sets the <see cref="HttpConstants.Headers.Authorization" /> header of the specified <see cref="message" />
    ///     to the <see cref="token" />
    /// </summary>
    public static void SetJWTBearerToken(this HttpRequestMessage message, string token)
    {
        if (token.HasNoValue())
        {
            return;
        }

        message.Headers.Add(HttpConstants.Headers.Authorization, $"{BearerTokenPrefix} {token}");
    }

    /// <summary>
    ///     Sets the <see cref="HttpConstants.Headers.RequestId" /> header of the specified <see cref="message" />
    ///     to the <see cref="ICallContext.CallId" />
    /// </summary>
    public static void SetRequestId(this HttpRequestMessage message, ICallContext context)
    {
        var callId = context.CallId;
        if (callId.HasNoValue())
        {
            return;
        }

        if (message.Headers.Contains(HttpConstants.Headers.RequestId))
        {
            return;
        }

        message.Headers.Add(HttpConstants.Headers.RequestId, context.CallId);
    }

    /// <summary>
    ///     Whether the specified HMAC signature represents the signature of the contents of the inbound request,
    ///     serialized by the method
    ///     <see cref="RequestExtensions.SerializeToJson(Infrastructure.Web.Api.Interfaces.IWebRequest?)" />
    /// </summary>
    public static async Task<bool> VerifyHMACSignatureAsync(this HttpRequest request, string signature, string secret,
        CancellationToken cancellationToken)
    {
        if (request.Body.Position != 0)
        {
            request.RewindBody();
        }

        var body = await request.Body.ReadFullyAsync(cancellationToken);
        request.RewindBody(); // HACK: need to do this for later middleware

        if (body.Length == 0)
        {
            body = Encoding.UTF8.GetBytes(RequestExtensions
                .EmptyRequestJson); //HACK: we assume that an empty JSON request was signed
        }

        var signer = new HMACSigner(body, secret);
        var verifier = new HMACVerifier(signer);

        return verifier.Verify(signature);
    }

    private static bool IsMediaType(this string? source, string? target)
    {
        if (source.HasNoValue() || target.HasNoValue())
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(source, out var sourceMediaType))
        {
            return false;
        }

        if (!MediaTypeHeaderValue.TryParse(target, out var targetMediaType))
        {
            return false;
        }

        return sourceMediaType.MediaType.EqualsIgnoreCase(targetMediaType.MediaType);
    }
}