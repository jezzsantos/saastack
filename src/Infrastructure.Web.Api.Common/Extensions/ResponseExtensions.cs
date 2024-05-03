using System.Net;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class ResponseExtensions
{
    /// <summary>
    ///     Returns the default <see cref="HttpStatusCode" /> for the specified <see cref="responseType" />
    ///     and     <see cref="options" />
    /// </summary>
    public static HttpStatusCode GetDefaultResponseCode(this HttpMethod method, ResponseCodeOptions options)
    {
        if (method == HttpMethod.Get)
        {
            return HttpStatusCode.OK;
        }

        if (method == HttpMethod.Post)
        {
            return options.HasLocation
                ? HttpStatusCode.Created
                : HttpStatusCode.OK;
        }

        if (method == HttpMethod.Put
            || method == HttpMethod.Patch)
        {
            return HttpStatusCode.Accepted;
        }

        if (method == HttpMethod.Delete)
        {
            return options.HasContent
                ? HttpStatusCode.Accepted
                : HttpStatusCode.NoContent;
        }

        return HttpStatusCode.OK;
    }
}