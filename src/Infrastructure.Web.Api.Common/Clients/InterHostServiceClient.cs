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
            modifiedRequestFilter = req =>
            {
                inboundRequestFilter(req);
                AddCorrelationId(req, context);
                AddCallerAuthorization(req, context, _privateInterHostSecret);
            };
        }
        else
        {
            modifiedRequestFilter = req =>
            {
                AddCorrelationId(req, context);
                AddCallerAuthorization(req, context, _privateInterHostSecret);
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
            message.SetAuthorization(context, privateInterHostSecret);
        }
    }
}