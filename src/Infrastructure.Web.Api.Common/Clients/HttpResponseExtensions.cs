using Application.Common;

namespace Infrastructure.Web.Api.Common.Clients;

public static class HttpResponseExtensions
{
    /// <summary>
    ///     Extracts the <see cref="RequestCorrelationFilter.ResponseHeaderName" /> header from the response,
    ///     or creates a new one
    /// </summary>
    public static string GetOrCreateRequestId(this HttpResponseMessage response)
    {
        string requestId;
        if (response.Headers.TryGetValues(HttpHeaders.RequestId, out var requestIds))
        {
            requestId = requestIds.FirstOrDefault() ?? Caller.GenerateCallId();
        }
        else
        {
            requestId = Caller.GenerateCallId();
        }

        return requestId;
    }
}