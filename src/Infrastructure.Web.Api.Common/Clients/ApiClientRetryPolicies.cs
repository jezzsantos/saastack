using System.Net;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Polly.Timeout;

namespace Infrastructure.Web.Api.Common.Clients;

public static class ApiClientRetryPolicies
{
    /// <summary>
    ///     Creates an exponential backoff policy with slightly random intervals
    ///     See https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#earlier-jitter-recommendations
    ///     and https://aws.amazon.com/blogs/architecture/exponential-backoff-and-jitter/
    /// </summary>
    public static AsyncRetryPolicy CreateRetryWithExponentialBackoffAndJitter(int retryCount = 4, int minDelayMs = 10,
        int maxDelayMs = 100)
    {
        var minimumDelay = TimeSpan.FromMilliseconds(minDelayMs);
        var maximumDelay = TimeSpan.FromMilliseconds(maxDelayMs);
        var delay = Backoff.AwsDecorrelatedJitterBackoff(minimumDelay, maximumDelay, retryCount);

        return Policy
            .Handle<HttpRequestException>(res => IsRetryStatusCode(res.StatusCode.GetValueOrDefault(HttpStatusCode.OK)))
            .Or<TimeoutRejectedException>()
            .Or<BrokenCircuitException>()
            .Or<OperationCanceledException>()
            .OrInner<OperationCanceledException>()
            .WaitAndRetryAsync(delay);
    }

    private static bool IsRetryStatusCode(HttpStatusCode statusCode)
    {
        return (int)statusCode > 500
               || statusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
            ;
    }
}