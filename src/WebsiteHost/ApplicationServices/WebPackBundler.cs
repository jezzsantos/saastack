using System.Text.Json;
using Common.Extensions;

namespace WebsiteHost.ApplicationServices;

/// <summary>
///     Provides bundle data from WebPack
/// </summary>
public class WebPackBundler : IWebPackBundler
{
    private const string WebPackOutputLocation = @"ClientApp\webpack.build.json";

    public string GetBundleName(string basePath)
    {
        var webPackOutputFilePath = Path.Combine(basePath, WebPackOutputLocation);

        using var webPackOutputContent = File.OpenText(webPackOutputFilePath);
        var outputData = JsonSerializer.Deserialize<WebPackOutputJsonData>(webPackOutputContent.BaseStream);

        var bundleName = outputData?.Main?.Js;
        if (bundleName.HasValue())
        {
            return bundleName;
        }

        throw new InvalidOperationException(
            Resources.WebPackBundler_InvalidBundle.Format(WebPackOutputLocation));
    }
}