using System.Text.Json;
using System.Text.Json.Serialization;
using Common;
using Common.Extensions;
using Infrastructure.Web.Hosting.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using WebsiteHost.Models;

namespace WebsiteHost.Controllers;

public abstract class CSRFController : Controller
{
    private const string WebPackOutputLocation = @"ClientApp\webpack.build.json";
    private readonly CSRFMiddleware.ICSRFService _csrfService;
    private readonly IHostEnvironment _hostEnvironment;

    protected CSRFController(IHostEnvironment hostEnvironment, CSRFMiddleware.ICSRFService csrfCSRFService)
    {
        _hostEnvironment = hostEnvironment;
        _csrfService = csrfCSRFService;
    }

    protected IActionResult CSRFResult()
    {
        var userId = Request.GetUserIdFromAuthNCookie()
            .Match(optional => optional, _ => Optional<Optional<string>>.None);
        var csrfTokenPair = _csrfService.CreateTokens(userId);
        WriteSignatureToCookie(csrfTokenPair.Signature);
        var bundleName = GetWebPackBundleName();

        var model = new IndexSpaPage
        {
#if TESTINGONLY
            IsTestingOnly = true,
#else
            IsTestingOnly = false,
#endif
#if HOSTEDONAZURE
            IsHostedOn = "AZURE",
#elif HOSTEDONAWS
            IsHostedOn = "AWS",
#else
            IsHostedOn = "UNKNOWN",
#endif
            CSRFFieldName = CSRFConstants.Html.CSRFRequestFieldName,
            CSRFHeaderToken = CSRFToken(),
            JsBundleName = bundleName
        };
        return View(model);
    }

    private string GetWebPackBundleName()
    {
        var applicationBasePath = _hostEnvironment.ContentRootPath;
        var webPackOutputFilePath = Path.Combine(applicationBasePath, WebPackOutputLocation);

        using var webPackOutputContent = System.IO.File.OpenText(webPackOutputFilePath);
        var outputData = JsonSerializer.Deserialize<WebPackOutputJsonData>(webPackOutputContent.BaseStream);

        var bundleName = outputData?.Main?.Js;
        if (bundleName.HasValue())
        {
            return bundleName;
        }

        throw new InvalidOperationException(
            $"Webpack output file '{WebPackOutputLocation}' was not found in the project. Please run `npm build` to produce this output file from WebPack");
    }

    private string CSRFToken()
    {
        var userId = Request.GetUserIdFromAuthNCookie()
            .Match(optional => optional, _ => Optional<Optional<string>>.None);
        var csrfTokenPair = _csrfService.CreateTokens(userId);
        WriteSignatureToCookie(csrfTokenPair.Signature);

        return csrfTokenPair.Token;
    }

    private void WriteSignatureToCookie(string signature)
    {
        Response.Cookies.Append(CSRFConstants.Cookies.AntiCSRF, signature, new CookieOptions
        {
            Secure = true,
            HttpOnly = true,
            Expires = DateTime.UtcNow.Add(CSRFConstants.Cookies.DefaultCSRFExpiry),
            SameSite = SameSiteMode.Lax
        });
    }
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