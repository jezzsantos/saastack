using System.Net.Http.Json;
using System.Text.Json;
using Application.Common;
using Common.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class HttpResponseExtensions
{
    /// <summary>
    ///     Returns the <see cref="ProblemDetails" /> from the specified <see cref="response" />
    /// </summary>
    public static async Task<ProblemDetails?> AsProblemAsync(this HttpResponseMessage response,
        JsonSerializerOptions jsonOptions)
    {
        var contentType = response.Content.Headers.ContentType;
        if (contentType.Exists()
            && contentType.MediaType == HttpContentTypes.JsonProblem)
        {
            return await response.Content.ReadFromJsonAsync<ProblemDetails>(jsonOptions,
                CancellationToken.None);
        }

        return null;
    }

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