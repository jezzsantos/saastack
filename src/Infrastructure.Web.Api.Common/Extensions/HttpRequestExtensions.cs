using System.Net.Http.Headers;
using System.Text;
using Application.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class HttpRequestExtensions
{
    public const string BearerTokenPrefix = "Bearer";

    /// <summary>
    ///     Returns the value of the APIKEY authorization from the request (if any)
    /// </summary>
    public static Optional<string> GetAPIKeyAuth(this HttpRequest request)
    {
        var fromQuery = request.Query[HttpQueryParams.APIKey].FirstOrDefault();
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
    /// <param name="request"></param>
    /// <returns></returns>
    public static (Optional<string> Username, Optional<string> Password) GetBasicAuth(this HttpRequest request)
    {
        var fromBasicAuth = AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var result)
            ? result
            : null;
        if (!fromBasicAuth.Exists())
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
        var authorization = request.Headers[HttpHeaders.HMACSignature];
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
    ///     Rewinds the <see cref="HttpRequest.Body" /> back to the start
    /// </summary>
    public static void RewindBody(this HttpRequest httpRequest)
    {
        if (httpRequest.Body.CanSeek)
        {
            httpRequest.Body.Seek(0, SeekOrigin.Begin);
        }
    }

    /// <summary>
    ///     Sets the <see cref="HttpHeaders.Authorization" /> header of the specified <see cref="message" />
    ///     to the <see cref="apiKey" />
    /// </summary>
    public static void SetAPIKey(this HttpRequestMessage message, string apiKey)
    {
        if (apiKey.HasNoValue())
        {
            return;
        }

        message.Headers.Add(HttpHeaders.Authorization,
            $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:"))}");
    }

    /// <summary>
    ///     Sets the <see cref="ICallerContext.Authorization" /> to the specified <see cref="message" />
    /// </summary>
    public static void SetAuthorization(this HttpRequestMessage message, ICallerContext context)
    {
        var authorization = context.Authorization;
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
    ///     Sets the HMAC signature header on the specified <see cref="message" /> by signing the body of the specified
    ///     <see cref="request" />
    /// </summary>
    public static void SetHMACAuth(this HttpRequestMessage message, IWebRequest request, string secret)
    {
        var signature = request.CreateHMACSignature(secret);

        message.Headers.Add(HttpHeaders.HMACSignature, signature);
    }

    /// <summary>
    ///     Sets the <see cref="HttpHeaders.Authorization" /> header of the specified <see cref="message" />
    ///     to the <see cref="token" />
    /// </summary>
    public static void SetJWTBearerToken(this HttpRequestMessage message, string token)
    {
        if (token.HasNoValue())
        {
            return;
        }

        message.Headers.Add(HttpHeaders.Authorization, $"{BearerTokenPrefix} {token}");
    }

    /// <summary>
    ///     Sets the <see cref="HttpHeaders.RequestId" /> header of the specified <see cref="message" />
    ///     to the <see cref="ICallContext.CallId" />
    /// </summary>
    public static void SetRequestId(this HttpRequestMessage message, ICallContext context)
    {
        var callId = context.CallId;
        if (callId.HasNoValue())
        {
            return;
        }

        if (message.Headers.Contains(HttpHeaders.RequestId))
        {
            return;
        }

        message.Headers.Add(HttpHeaders.RequestId, context.CallId);
    }

    /// <summary>
    ///     Whether the specified HMAC signature represents the signature of the contents of the inbound request,
    /// signed by the method <see cref="RequestExtensions.SerializeToJson"/>
    /// </summary>
    public static async Task<bool> VerifyHMACSignatureAsync(this HttpRequest request, string signature, string secret,
        CancellationToken cancellationToken)
    {
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
}