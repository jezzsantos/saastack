using Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Mvc;
using WebsiteHost.ApplicationServices;
using WebsiteHost.Models;

namespace WebsiteHost.Controllers;

public abstract class CSRFController : Controller
{
    private readonly CSRFMiddleware.ICSRFService _csrfService;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IWebPackBundler _webpackBundler;

    protected internal CSRFController(IHostEnvironment hostEnvironment, CSRFMiddleware.ICSRFService csrfService,
        IWebPackBundler webpackBundler)
    {
        _hostEnvironment = hostEnvironment;
        _csrfService = csrfService;
        _webpackBundler = webpackBundler;
    }

    protected CSRFController(IHostEnvironment hostEnvironment, CSRFMiddleware.ICSRFService csrfService) : this(
        hostEnvironment, csrfService, new WebPackBundler())
    {
    }

    protected IActionResult CSRFResult()
    {
        var userId = Request.GetUserIdFromAuthNCookie()
            .Match(optional => optional.Value, _ => Optional<string>.None);
        var csrfTokenPair = _csrfService.CreateTokens(userId);
        WriteCSRFCookie(csrfTokenPair.Signature, userId);
        var bundleName = GetWebPackBundleName();

        var model = new IndexSpaPage
        {
#if TESTINGONLY
            IsTestingOnly = true,
#else
            IsTestingOnly = false,
#endif
#if HOSTEDONPREMISES
            IsHostedOn = "ONPREMISES",
#elif HOSTEDONAZURE
            IsHostedOn = "AZURE",
#elif HOSTEDONAWS
            IsHostedOn = "AWS",
#else
            IsHostedOn = "UNKNOWN",
#endif
            CSRFFieldName = CSRFConstants.Html.CSRFRequestFieldName,
            CSRFHeaderToken = csrfTokenPair.Token,
            JsBundleName = bundleName
        };
        return View(model);
    }

    private string GetWebPackBundleName()
    {
        var applicationBasePath = _hostEnvironment.ContentRootPath;
        return _webpackBundler.GetBundleName(applicationBasePath);
    }

    private void WriteCSRFCookie(string signature, Optional<string> userId)
    {
        var cookieValue = new CSRFMiddleware.CSRFCookie(
            userId.HasValue
                ? userId.Value
                : null, signature).ToCookieValue();
        Response.Cookies.Append(CSRFConstants.Cookies.AntiCSRF, cookieValue, new CookieOptions
        {
            Secure = true,
            HttpOnly = true,
            Expires = DateTime.UtcNow.Add(CSRFConstants.Cookies.DefaultCSRFExpiry),
            SameSite = SameSiteMode.Lax
        });
    }
}