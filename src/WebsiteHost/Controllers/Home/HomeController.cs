using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Mvc;

namespace WebsiteHost.Controllers.Home;


public class HomeController : CSRFController
{
    public HomeController(IHostEnvironment hostEnvironment, CSRFMiddleware.ICSRFService csrfService) :
        base(hostEnvironment, csrfService)
    {
    }

    [HttpGet("error")]
    public IActionResult Error()
    {
        return Problem();
    }

    public IActionResult Index()
    {
        return CSRFResult();
    }
}