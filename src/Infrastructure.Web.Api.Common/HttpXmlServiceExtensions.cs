using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Api.Common;

public static class HttpXmlServiceExtensions
{
    /// <summary>
    ///     Configures options used for reading and writing XML
    /// </summary>
    public static IServiceCollection ConfigureHttpXmlOptions(this IServiceCollection services,
        Action<XmlOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }
}