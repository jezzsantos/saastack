using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Application.Common.Extensions;
using Application.Interfaces;
using Common.Extensions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Common.Extensions;
using Infrastructure.Web.Interfaces;

namespace Infrastructure.Web.Api.Common.Clients;

/// <summary>
///     A service client used to call between API hosts, with retries.
///     Adds both the <see cref="HttpConstants.Headers.RequestId" /> and <see cref="HttpConstants.Headers.Authorization" />
///     to all downstream requests
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InterHostServiceClient : ApiServiceClient
{
    private const int RetryCount = 2;
    private readonly string _privateInterHostSecret;
    private readonly string _hmacSecret;

    public InterHostServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl,
        string privateInterHostSecret, string hmacSecret) :
        base(clientFactory, jsonOptions, baseUrl, RetryCount)
    {
        _privateInterHostSecret = privateInterHostSecret;
        _hmacSecret = hmacSecret;
    }

    internal static void SetAuthorization(HttpRequestMessage message, ICallerContext caller,
        string privateInterHostSecret, string hmacSecret)
    {
        var authorization = caller.Authorization;
        if (!authorization.HasValue)
        {
            return;
        }

        var authorizationValue = authorization is { HasValue: true, Value.Value.HasValue: true }
            ? authorization.Value.Value.Value
            : null;

        switch (authorization.Value.Method)
        {
            case ICallerContext.AuthorizationMethod.Token:
            {
                if (authorizationValue.HasValue())
                {
                    var token = authorization.Value.Value.Value;
                    message.SetJWTBearerToken(token);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.APIKey:
            {
                if (authorizationValue.HasValue())
                {
                    var apiKey = authorization.Value.Value.Value;
                    message.SetAPIKey(apiKey);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.PrivateInterHost:
            {
                if (authorizationValue.HasValue())
                {
                    var token = authorization.Value.Value.Value;
                    message.SetPrivateInterHostAuth(privateInterHostSecret, token);
                }
                else
                {
                    message.SetPrivateInterHostAuth(privateInterHostSecret);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.HMAC:
            {
                if (authorizationValue.HasValue())
                {
                    var hmacSecret2 = authorization.Value.Value.Value;
                    message.SetHMACAuth(hmacSecret2);
                }
                else
                {
                    message.SetHMACAuth(hmacSecret);
                }

                break;
            }

            case ICallerContext.AuthorizationMethod.AuthNCookie:
            {
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override JsonClient CreateJsonClient(ICallerContext? context,
        Action<HttpRequestMessage>? inboundRequestFilter,
        out Action<HttpRequestMessage> modifiedRequestFilter)
    {
        var client = new JsonClient(ClientFactory, JsonOptions);
        client.SetBaseUrl(BaseUrl);
        if (inboundRequestFilter.Exists())
        {
            modifiedRequestFilter = msg =>
            {
                inboundRequestFilter(msg);
                AddCorrelationId(msg, context);
                AddCallerAuthorization(msg, context, _privateInterHostSecret, _hmacSecret);
            };
        }
        else
        {
            modifiedRequestFilter = msg =>
            {
                AddCorrelationId(msg, context);
                AddCallerAuthorization(msg, context, _privateInterHostSecret, _hmacSecret);
            };
        }

        return client;
    }

    private static void AddCorrelationId(HttpRequestMessage message, ICallerContext? context)
    {
        if (context.Exists())
        {
            message.SetRequestId(context.ToCall());
        }
    }

    private static void AddCallerAuthorization(HttpRequestMessage message, ICallerContext? context,
        string privateInterHostSecret, string hmacSecret)
    {
        if (context.Exists())
        {
            SetAuthorization(message, context, privateInterHostSecret, hmacSecret);
        }
    }
}