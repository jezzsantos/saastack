using Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Mvc;
using WebsiteHost.Models;

namespace WebsiteHost.Controllers;

public abstract class CSRFController : Controller
{
    private readonly CSRFMiddleware.ICSRFService _csrfService;

    protected CSRFController(CSRFMiddleware.ICSRFService csrfCSRFService)
    {
        _csrfService = csrfCSRFService;
    }

    protected IActionResult CSRFResult()
    {
        var userId = Request.GetUserIdFromAuthNCookie()
            .Match(optional => optional, _ => Optional<Optional<string>>.None);
        var csrfTokenPair = _csrfService.CreateTokens(userId);
        WriteSignatureToCookie(csrfTokenPair.Signature);

        var model = new IndexSpaPage
        {
#if TESTINGONLY
            IsTestingOnly = true,
#else
            IsTestingOnly = false,
#endif
            CSRFFieldName = CSRFConstants.Html.CSRFRequestFieldName,
            CSRFHeaderToken = CSRFToken()
        };
        return View(model);
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