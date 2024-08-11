using System.Reflection;
using Common;
using Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Infrastructure.Web.Hosting.Common.Documentation;

public static class OperationFilterExtensions
{
    /// <summary>
    ///     Returns the request type for the given <paramref name="context" />.
    /// </summary>
    public static Optional<Type> GetRequestType(this OperationFilterContext context)
    {
        var requestParameters = context.MethodInfo.GetParameters()
            .Where(IsWebRequest)
            .ToList();
        if (requestParameters.HasNone())
        {
            return Optional<Type>.None;
        }

        return requestParameters.First().ParameterType;

        static bool IsWebRequest(ParameterInfo requestParameter)
        {
            var type = requestParameter.ParameterType;
            if (type.NotExists())
            {
                return false;
            }

            return typeof(IWebRequest).IsAssignableFrom(type);
        }
    }
}