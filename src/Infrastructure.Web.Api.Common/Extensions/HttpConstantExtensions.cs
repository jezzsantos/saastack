using System.Net;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class HttpConstantExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="HttpStatusCode" /> to a <see cref="StatusCode" />
    /// </summary>
    public static StatusCode ToStatusCode(this HttpStatusCode code)
    {
        return new StatusCode(code);
    }
}