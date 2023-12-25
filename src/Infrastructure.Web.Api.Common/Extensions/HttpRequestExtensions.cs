using Application.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class HttpRequestExtensions
{
    
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
    ///     to the <see cref="ICallerContext.Authorization" />
    /// </summary>
    public static void SetBearerToken(this HttpRequestMessage message, ICallerContext context)
    {
        var token = context.Authorization;
        if (token.HasNoValue())
        {
            return;
        }

        message.Headers.Add(HttpHeaders.Authorization, $"Bearer: {token}");
    }

    /// <summary>
    ///     Sets the HMAC signature header on the specified <see cref="message" /> by signing the specified
    ///     <see cref="request" />
    /// </summary>
    public static void SetHmacAuth(this HttpRequestMessage message, IWebRequest request, string secret)
    {
        var signature = request.CreateHmacSignature(secret);

        message.Headers.Add(HttpHeaders.HmacSignature, signature);
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
}