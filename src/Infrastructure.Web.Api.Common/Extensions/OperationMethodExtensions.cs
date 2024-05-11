using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class OperationMethodExtensions
{
    /// <summary>
    ///     Converts the <see cref="OperationMethod" /> to an appropriate <see cref="HttpMethod" />
    /// </summary>
    public static HttpMethod ToHttpMethod(this OperationMethod method)
    {
        return method switch
        {
            OperationMethod.Get => HttpMethod.Get,
            OperationMethod.Search => HttpMethod.Get,
            OperationMethod.Post => HttpMethod.Post,
            OperationMethod.PutPatch => HttpMethod.Put,
            OperationMethod.Delete => HttpMethod.Delete,
            _ => HttpMethod.Get
        };
    }

    /// <summary>
    ///     Whether the specified <see cref="method" /> could have a request body
    /// </summary>
    public static bool CanHaveBody(this OperationMethod method)
    {
        return method switch
        {
            OperationMethod.Post => true,
            OperationMethod.PutPatch => true,
            _ => false
        };
    }
}