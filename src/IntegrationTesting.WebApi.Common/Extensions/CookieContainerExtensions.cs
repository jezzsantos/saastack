using System.Net;

namespace IntegrationTesting.WebApi.Common.Extensions;

public static class CookieContainerExtensions
{
    /// <summary>
    ///     Clears all the cookies from the request.
    ///     Note: this expires all the cookies, it doesn't remove them from the container.
    /// </summary>
    public static void Clear(this CookieContainer container, IHttpClient client)
    {
        container.GetCookies(client.BaseAddress!)
            .ToList()
            .ForEach(c => c.Expired = true);
    }
}