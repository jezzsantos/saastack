using Microsoft.AspNetCore.Http;

namespace Infrastructure.Web.Common.Extensions;

public static class RequestExtensions
{
    /// <summary>
    ///     Returns a convenient representation of the request
    /// </summary>
    public static string ToDisplayName(this HttpRequest request)
    {
        var path = request.Path;
        var method = request.Method;
        var accept = request.Headers.Accept.ToString();

        return $"{method} {path} ({accept})";
    }
}