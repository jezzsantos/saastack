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

    public InterHostServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl,
        string privateInterHostSecret) :
        base(clientFactory, jsonOptions, baseUrl, RetryCount)
    {
        _privateInterHostSecret = privateInterHostSecret;
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
                AddCallerAuthorization(msg, context, _privateInterHostSecret);
            };
        }
        else
        {
            modifiedRequestFilter = msg =>
            {
                AddCorrelationId(msg, context);
                AddCallerAuthorization(msg, context, _privateInterHostSecret);
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
        string privateInterHostSecret)
    {
        if (context.Exists())
        {
            SetAuthorization(message, context, privateInterHostSecret);
        }
    }

    internal static void SetAuthorization(HttpRequestMessage message, ICallerContext caller,
        string privateInterHostSecret)
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
                //We don't expect this client to be used to forward maintenance service workloads 
                throw new NotSupportedException(Resources.RequestExtensions_HMACAuthorization_NotSupported);
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}