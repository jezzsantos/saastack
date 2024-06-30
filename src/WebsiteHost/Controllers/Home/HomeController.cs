using System.Text;
using Common.Extensions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace WebsiteHost.Controllers.Home;

public class HomeController : CSRFController
{
    private const string IndexHtmlFileName = "index.html";
    private static string? _cachedIndexHtml;
    private readonly IWebHostEnvironment _hostEnvironment;

    public HomeController(IWebHostEnvironment hostEnvironment, CSRFMiddleware.ICSRFService csrfService) :
        base(csrfService)
    {
        _hostEnvironment = hostEnvironment;
        EnsureWebsiteIsBuilt();
    }

    [HttpGet("error")]
    public IActionResult Error()
    {
        return Problem();
    }

    public IActionResult Index()
    {
        var pageHtml = GetCachedHtml();
        pageHtml = WriteCompilationOptionsToHtml(pageHtml);

        return CSRFResult(pageHtml);
    }

    private void EnsureWebsiteIsBuilt()
    {
        var indexHtmlPath = GetIndexHtmlPath();
        if (!System.IO.File.Exists(indexHtmlPath))
        {
            throw new InvalidOperationException(Resources.HomeController_IndexPageNotBuilt.Format(IndexHtmlFileName));
        }
    }

    private string GetCachedHtml()
    {
        if (_cachedIndexHtml.NotExists())
        {
            var indexHtmlPath = GetIndexHtmlPath();
            _cachedIndexHtml =
                System.IO.File.ReadAllText(indexHtmlPath);
        }

        return _cachedIndexHtml;
    }

    private string GetIndexHtmlPath()
    {
        var rootPath = _hostEnvironment.WebRootPath;
        return Path.GetFullPath(Path.Combine(rootPath, IndexHtmlFileName));
    }

    private static string WriteCompilationOptionsToHtml(string pageHtml)
    {
        const string endHeadTag = "</head>";
        var html = pageHtml;

        var endHeadTagIndex = html.IndexOf(endHeadTag, StringComparison.OrdinalIgnoreCase);
        if (endHeadTagIndex > -1)
        {
            var scriptToAdd = new StringBuilder("<script>");
#if TESTINGONLY
            scriptToAdd.Append("var isTestingOnly=true;");
#else
            scriptToAdd.Append("var isTestingOnly=false;");
#endif
#if HOSTEDONAZURE
            scriptToAdd.Append("var isHostedOn=\"AZURE\";");
#elif HOSTEDONAWS
            scriptToAdd.Append("var isHostedOn=\"AWS\";");
#endif

            scriptToAdd.Append("</script>");

            var javascript = scriptToAdd.ToString();
            html = html.Insert(endHeadTagIndex + endHeadTag.Length, javascript);
        }

        return html;
    }
}