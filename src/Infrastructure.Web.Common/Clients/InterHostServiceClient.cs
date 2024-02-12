using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Application.Common;
using Application.Interfaces;
using Common.Extensions;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Common.Extensions;

namespace Infrastructure.Web.Common.Clients;

/// <summary>
///     A service client used to call between API hosts, with retries.
///     Adds both the <see cref="HttpHeaders.RequestId" /> and <see cref="HttpHeaders.Authorization" />
///     to all downstream requests
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InterHostServiceClient : ApiServiceClient
{
    private const int RetryCount = 2;

    public InterHostServiceClient(IHttpClientFactory clientFactory, JsonSerializerOptions jsonOptions, string baseUrl) :
        base(clientFactory, jsonOptions, baseUrl, RetryCount)
    {
    }

    protected override JsonClient CreateJsonClient(ICallerContext context,
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
                AddCallerAuthorization(req, context);
            };
        }
        else
        {
            modifiedRequestFilter = req =>
            {
                AddCorrelationId(req, context);
                AddCallerAuthorization(req, context);
            };
        }

        return client;
    }

    private static void AddCorrelationId(HttpRequestMessage message, ICallerContext context)
    {
        message.SetRequestId(context.ToCall());
    }

    private static void AddCallerAuthorization(HttpRequestMessage message, ICallerContext context)
    {
        message.SetAuthorization(context);
    }
}