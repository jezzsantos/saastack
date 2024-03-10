using ApiHost1;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using JetBrains.Annotations;

var modules = HostedModules.Get();

var app = WebApplication.CreateBuilder(args)
    .ConfigureApiHost(modules, WebHostOptions.BackEndAncillaryApiHost);
app.Run();

namespace ApiHost1
{
    [UsedImplicitly]
    public class Program;
}