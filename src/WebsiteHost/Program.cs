using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using JetBrains.Annotations;
using WebsiteHost;

var modules = HostedModules.Get();

var app = WebApplication.CreateBuilder(args)
    .ConfigureApiHost(modules, WebHostOptions.BackEndForFrontEndWebHost);
app.Run();

namespace WebsiteHost
{
    [UsedImplicitly]
    public class Program
    {
    }
}