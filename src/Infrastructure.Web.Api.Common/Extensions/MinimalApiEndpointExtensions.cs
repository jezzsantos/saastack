using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Infrastructure.Web.Api.Common.Extensions;

public static class MinimalApiEndpointExtensions
{
    /// <summary>
    ///     Provides an extension method to add an authorization assertion to an endpoint
    /// </summary>
    public static TBuilder RequireCallerAuthorization<TBuilder>(this TBuilder builder, string policyName)
        where TBuilder : IEndpointConventionBuilder
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.RequireAuthorization(new AuthorizeAttribute(policyName));
    }
}