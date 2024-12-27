using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace WebsiteHost.ApplicationServices;

/// <summary>
///     Defines a bundler for WebPack output data.
/// </summary>
public interface IWebPackBundler
{
    /// <summary>
    ///     Retrieves the bundle name
    /// </summary>
    string GetBundleName(string basePath);
}

[UsedImplicitly]
public class WebPackOutputJsonData
{
    [JsonPropertyName("main")] public Bundle? Main { get; set; }
}

[UsedImplicitly]
public class Bundle
{
    [JsonPropertyName("js")] public string? Js { get; set; }
}