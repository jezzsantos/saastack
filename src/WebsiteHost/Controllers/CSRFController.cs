using System.Text;
using Common;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace WebsiteHost.Controllers;

public abstract class CSRFController : Controller
{
    private readonly CSRFMiddleware.ICSRFService _csrfService;

    protected CSRFController(CSRFMiddleware.ICSRFService csrfCSRFService)
    {
        _csrfService = csrfCSRFService;
    }

    protected IActionResult CSRFResult(string pageHtml)
    {
        var userId = Request.GetUserIdFromAuthNCookie()
            .Match(optional => optional, _ => Optional<Optional<string>>.None);
        var csrfTokenPair = _csrfService.CreateTokens(userId);
        WriteSignatureToCookie(csrfTokenPair.Signature);
        var contents = WriteTokenToHtmlMetadata(pageHtml, csrfTokenPair.Token);

        return File(contents, HttpContentTypes.Html);
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

    private static byte[] WriteTokenToHtmlMetadata(string pageHtml, string token)
    {
        var html = pageHtml;
        html = html
            .Replace(CSRFConstants.Html.CSRFFieldNamePlaceholder, CSRFConstants.Html.CSRFRequestFieldName)
            .Replace(CSRFConstants.Html.CSRFTokenPlaceholder, token);

        return Encoding.UTF8.GetBytes(html);
    }
}